extends VehicleBody3D


var power = 400
var turn_power = 500
var left
var right
var center_of_mass_marker: MeshInstance3D
var current_camera = 0
var cameras

# UI elements
var info_label: Label

# Logging
const LOG_FILE_PATH = "user://m27_log.csv" # note this will be in the user dir (search up where that is for ur machine)
var log_file: FileAccess

var help_menu_scene = preload("res://help_menu.tscn")
var help_menu_instance = null
var help_button: Button

var restart_button: Button
var quit_button: Button

var selected_map = ""
var config_file = "user://settings.cfg"
var config = ConfigFile.new()

func load_config():
	var err = config.load(config_file)
	if err != OK:
		print("No config file found. Creating a new one.")
		create_default_config()
	else:
		print("Config file loaded successfully.")
		load_settings_from_config()

func load_settings_from_config():
	selected_map = config.get_value("Map", "selected")
	# Load other settings as needed

func create_default_config():
	config.set_value("Map", "selected", "res://maps/original.tscn")
	config.set_value("Physics", "wheel_mass", 1.0)
	config.set_value("Physics", "body_mass", 1.0)
	config.set_value("Physics", "arm_body_mass", 1.0)
	config.save(config_file)

func _ready():
	load_config()
	# Create a small sphere mesh for the center of mass marker
	center_of_mass_marker = MeshInstance3D.new()
	center_of_mass_marker.mesh = SphereMesh.new()
	center_of_mass_marker.mesh.radius = 0.1  # Adjust for visibility
	center_of_mass_marker.material_override = StandardMaterial3D.new()
	center_of_mass_marker.material_override.albedo_color = Color(1, 0, 0)  # Set color to red

	# Add marker as a child of the VehicleBody3D
	add_child(center_of_mass_marker)

	# Get the existing info_label
	info_label = $CanvasLayer_UI/infoLabel
	print_scene_tree()
	
	# Initialize logging
	initialize_logging()
	create_help_button()
	create_restart_button()
	create_quit_button()

func _process(delta):
	# Get the current center of mass position
	var com_position = get_center_of_mass()
	
	# Update the position of the center of mass marker in local space
	center_of_mass_marker.transform.origin = com_position
	
	#if Input.is_action_just_pressed("toggle_view"):
		#toggle_camera_view()
	
	update_info_label()
	log_data()

func _physics_process(delta: float) -> void:
	# get keyboard
	left = int((Input.get_action_raw_strength("left_tread_forward")
		- Input.get_action_raw_strength("left_tread_backward")) * 10)
	right = int((Input.get_action_raw_strength("right_tread_forward")
		- Input.get_action_raw_strength("right_tread_backward")) * 10)
	
	# handle only one input
	if not left and right:
		$right_middle.engine_force = right * turn_power
	elif left and not right:
		$left_middle.engine_force = left * turn_power
	elif (left < 0 and right > 0) or (left > 0 and right < 0):
		$left_middle.engine_force = left * turn_power
		$right_middle.engine_force = right * turn_power
	else:
		$left_middle.engine_force = left * power
		$right_middle.engine_force = right * power
		

	
	#print($left_middle.engine_force, $right_middle.engine_force)

func print_scene_tree():
	print_node(self, 0)

func print_node(node: Node, indent: int):
	var indent_string = "  ".repeat(indent)
	print(indent_string + node.name + " (" + node.get_class() + ")")
	for child in node.get_children():
		print_node(child, indent + 1)

func initialize_logging():
	log_file = FileAccess.open(LOG_FILE_PATH, FileAccess.WRITE)
	if log_file:
		log_file.store_line("Timestamp,Position_X,Position_Y,Position_Z,Angle,IsFlipped,Velocity_X,Velocity_Y,Velocity_Z")
	else:
		print("Failed to open log file")

func is_upright() -> float:
	var object_up = global_transform.basis.y.normalized()
	var world_up = Vector3.UP
	var angle = acos(object_up.dot(world_up))
	return rad_to_deg(angle)

func update_info_label():
	if info_label:
		var angle_relative_to_ground = is_upright()
		var is_flipped = angle_relative_to_ground > 45
		var velocity = linear_velocity
		var position = global_transform.origin
		
		var info_text = "Position: (%.2f, %.2f, %.2f)\n" % [position.x, position.y, position.z]
		info_text += "Angle Relative to Ground: %.2f\n" % angle_relative_to_ground
		info_text += "Flipped Over: %s\n" % str(is_flipped)
		info_text += "Velocity: (%.2f, %.2f, %.2f)" % [velocity.x, velocity.y, velocity.z]
		
		info_label.text = info_text
	else:
		print("Info label not found")

func log_data():
	var log_file = FileAccess.open(LOG_FILE_PATH, FileAccess.READ_WRITE)
	if log_file:
		var angle_relative_to_ground = is_upright()
		var is_flipped = angle_relative_to_ground > 45
		var velocity = linear_velocity
		var position = global_transform.origin
		
	
		var current_time_us = Time.get_ticks_usec()
		
	
		var datetime = Time.get_datetime_dict_from_system()
		var microseconds = current_time_us % 1000000
		var datetime_string = "%04d-%02d-%02d %02d:%02d:%02d.%06d" % [
			datetime.year, datetime.month, datetime.day,
			datetime.hour, datetime.minute, datetime.second,
			microseconds
		]
		
		var log_entry = "%s,%.2f,%.2f,%.2f,%.2f,%s,%.2f,%.2f,%.2f" % [
			datetime_string,
			position.x, position.y, position.z,
			angle_relative_to_ground, str(is_flipped),
			velocity.x, velocity.y, velocity.z
		]
		
		# Janky way of saving data every iteration because if the game prematurely end the files doesn't close
		log_file.seek_end()  
		log_file.store_line(log_entry)
		log_file.flush() 
		log_file.close()  
		#print(log_entry)  
	else:
		print("Failed to open log file")

func create_help_button():
	help_button = Button.new()
	help_button.text = "Help"
	help_button.pressed.connect(_on_help_button_pressed)
	
	help_button.anchor_left = 1
	help_button.anchor_top = 0
	help_button.anchor_right = 1
	help_button.anchor_bottom = 0
	help_button.offset_left = -100 
	help_button.offset_top = 10
	help_button.offset_right = -10
	help_button.offset_bottom = 40  
	
	$CanvasLayer_UI.add_child(help_button)

func _on_help_button_pressed():
	if help_menu_instance == null:
		help_menu_instance = help_menu_scene.instantiate()
		add_child(help_menu_instance)
		
		if help_menu_instance is Control:
			help_menu_instance.anchor_left = 0.5
			help_menu_instance.anchor_top = 0.5
			help_menu_instance.anchor_right = 0.5
			help_menu_instance.anchor_bottom = 0.5
			help_menu_instance.offset_left = -help_menu_instance.size.x / 2
			help_menu_instance.offset_top = -help_menu_instance.size.y / 2
			help_menu_instance.offset_right = help_menu_instance.size.x / 2
			help_menu_instance.offset_bottom = help_menu_instance.size.y / 2
	else:
		help_menu_instance.queue_free()
		help_menu_instance = null


func create_restart_button():
	restart_button = Button.new()
	restart_button.text = "Restart"
	restart_button.pressed.connect(_on_restart_button_pressed)

	restart_button.anchor_left = 1
	restart_button.anchor_top = 0
	restart_button.anchor_right = 1
	restart_button.anchor_bottom = 0
	restart_button.offset_left = -100  
	restart_button.offset_right = -10
	restart_button.offset_top = 50 
	restart_button.offset_bottom = 80  
	
	$CanvasLayer_UI.add_child(restart_button)

func _on_restart_button_pressed():

	if help_menu_instance:
		help_menu_instance.queue_free()
	
	
	get_tree().change_scene_to_file(selected_map)

func create_quit_button():
	quit_button = Button.new()
	quit_button.text = "Quit"
	quit_button.pressed.connect(_on_quit_button_pressed)

	quit_button.anchor_left = 1
	quit_button.anchor_top = 0
	quit_button.anchor_right = 1
	quit_button.anchor_bottom = 0
	quit_button.offset_left = -100  
	quit_button.offset_right = -10
	quit_button.offset_top = 90
	quit_button.offset_bottom = 80  
	
	$CanvasLayer_UI.add_child(quit_button)
	
func _on_quit_button_pressed():

	if help_menu_instance:
		help_menu_instance.queue_free()
	
	
	get_tree().change_scene_to_file("../menu.tscn")

func _exit_tree():
	if log_file:
		log_file.close()

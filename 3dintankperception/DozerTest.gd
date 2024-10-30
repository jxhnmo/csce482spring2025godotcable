extends VehicleBody3D


var speed = 3
var left
var right
var center_of_mass_marker: MeshInstance3D
var current_camera = 0
var cameras

# UI elements
var info_label: Label

# Logging
const LOG_FILE_PATH = "user://tank_log.csv" # note this will be in the user dir (search up where that is for ur machine)
var log_file: FileAccess

func _ready():
	
	# Create a small sphere mesh for the center of mass marker
	center_of_mass_marker = MeshInstance3D.new()
	center_of_mass_marker.mesh = SphereMesh.new()
	center_of_mass_marker.mesh.radius = 0.1  # Adjust for visibility
	center_of_mass_marker.material_override = StandardMaterial3D.new()
	center_of_mass_marker.material_override.albedo_color = Color(1, 0, 0)  # Set color to red

	# Add marker as a child of the VehicleBody3D
	add_child(center_of_mass_marker)
	
	$FirstPersonCamera.current = true
	$ThirdPersonCamera.current = false
	$AuxCamera.current = false
	
	cameras = [$FirstPersonCamera, $ThirdPersonCamera, $AuxCamera]
	
	# Get the existing info_label
	info_label = $CanvasLayer_UI/infoLabel
	print_scene_tree()
	# Initialize logging
	initialize_logging()

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

func _process(delta):
	# Get the current center of mass position
	var com_position = get_center_of_mass()
	
	# Update the position of the center of mass marker in local space
	center_of_mass_marker.transform.origin = com_position
	
	if Input.is_action_just_pressed("toggle_view"):
		toggle_camera_view()
	
	update_info_label()
	log_data()

func toggle_camera_view():
	cameras[current_camera].current = false
	current_camera = (current_camera + 1) % 3
	cameras[current_camera].current = true

func _physics_process(delta: float) -> void:
	# get keyboard
	left = int((Input.get_action_raw_strength("left_tread_forward")
		- Input.get_action_raw_strength("left_tread_backward")) * 10)
	right = int((Input.get_action_raw_strength("right_tread_forward")
		- Input.get_action_raw_strength("right_tread_backward")) * 10)
	
	# handle only one input
	if not left and right:
		$LeftWheel.engine_force = right * speed
	elif left and not right:
		$RightWheel.engine_force = left * speed
	else:
		$LeftWheel.engine_force = left * speed
		$RightWheel.engine_force = right * speed

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
	log_file = FileAccess.open(LOG_FILE_PATH, FileAccess.WRITE)
	if log_file:
		var angle_relative_to_ground = is_upright()
		var is_flipped = angle_relative_to_ground > 45
		var velocity = linear_velocity
		var position = global_transform.origin
		
		var log_entry = "%s,%.2f,%.2f,%.2f,%.2f,%s,%.2f,%.2f,%.2f" % [
			Time.get_datetime_string_from_system(),
			position.x, position.y, position.z,
			angle_relative_to_ground, str(is_flipped),
			velocity.x, velocity.y, velocity.z
		]
		
		log_file.store_line(log_entry)
		print(log_entry)  # Log to stdout as well

func _exit_tree():
	if log_file:
		log_file.close()

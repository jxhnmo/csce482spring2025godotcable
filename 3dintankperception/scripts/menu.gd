extends Control

var option_button
var description_label
var selected_map = "res://maps/original.tscn"

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

func save_config():
	config.set_value("Map", "selected", selected_map)
	# Save other settings as needed
	config.save(config_file)

func _ready():
	load_config()
	option_button = OptionButton.new()
	$CenterContainer/VBoxContainer.add_child(option_button)
	
	description_label = Label.new()
	$CenterContainer/VBoxContainer.add_child(description_label)
	
	option_button.position = Vector2(100, 100)
	option_button.custom_minimum_size = Vector2(200, 50)
	
	description_label.position = Vector2(200, 200)

	option_button.add_item("Map Original")
	option_button.add_item("Map Squares")
	option_button.add_item("Map Squares 2")
	option_button.add_item("Map Lumpy")
	option_button.add_item("Map Crater")
	option_button.add_item("Map Big")
	option_button.add_item("Map Ramps")

	option_button.connect("item_selected", Callable(self, "_on_OptionButton_item_selected"))

	if not $CenterContainer/VBoxContainer/StartButton.pressed.is_connected(Callable(self, "_on_StartButton_pressed")):
		$CenterContainer/VBoxContainer/StartButton.pressed.connect(_on_StartButton_pressed)
	
	# Connect Help and Quit buttons
	if not $CenterContainer/VBoxContainer/HelpButton.pressed.is_connected(Callable(self, "_on_HelpButton_pressed")):
		$CenterContainer/VBoxContainer/HelpButton.pressed.connect(_on_HelpButton_pressed)
	
	if not $CenterContainer/VBoxContainer/QuitButton.pressed.is_connected(Callable(self, "_on_QuitButton_pressed")):
		$CenterContainer/VBoxContainer/QuitButton.pressed.connect(_on_QuitButton_pressed)
		
	#if not $VBoxContainer/ConfigButton.pressed.is_connected(Callable(self, "_on_ConfigButton_pressed")):
		#$VBoxContainer/ConfigButton.pressed.connect(_on_ConfigButton_pressed)


func _on_OptionButton_item_selected(index):
	var selected_text = option_button.get_item_text(index)
	print("Selected: " + selected_text)
	match selected_text:
		"Map Original":
			selected_map = "res://maps/original.tscn"
		"Map Squares":
			selected_map = "res://maps/squares.tscn"
		"Map Squares 2":
			selected_map = "res://maps/squares2.tscn"
		"Map Lumpy":
			selected_map = "res://maps/lumpy.tscn"
		"Map Crater":
			selected_map = "res://maps/crater.tscn"
		"Map Big":
			selected_map = "res://maps/big.tscn"
		"Map Ramps":
			selected_map = "res://maps/ramps.tscn"
		
		_:
			selected_map = config.get_value("Map", "selected")
			#selected_map = "res://maps/original.tscn"  

func _on_StartButton_pressed():
	save_config()
	print("Loading map: " + selected_map)
	var result = get_tree().change_scene_to_file(selected_map)
	if result != OK:
		print("Failed to change scene. Error code: ", result)
	else:
		print("Scene change initiated successfully")

func _on_HelpButton_pressed():
	# Change to the Help menu scene
	var result = get_tree().change_scene_to_file("res://help_menu.tscn")
	if result != OK:
		print("Failed to open Help menu. Error code: ", result)
	else:
		print("Help menu opened successfully")

#func _on_ConfigButton_pressed():
	#var config_menu = preload("res://config_menu.tscn").instantiate()
	#config_menu.config_saved.connect(_on_config_saved)
	#config_menu.config_closed.connect(_on_config_closed)
	#get_tree().root.add_child(config_menu)
	#self.hide()  # Hide the main menu
#
#func _on_config_saved(wheel_mass, body_mass, arm_body_mass):
	#print("Configuration saved. Wheel mass: ", wheel_mass, " Body mass: ", body_mass, " Arm body mass: ", arm_body_mass)
	## Apply these values to your game objects here
	#self.show()  # Show the main menu again

func _on_config_closed():
	print("Config menu closed without saving")
	self.show()  # Show the main menu again

func _on_QuitButton_pressed():
	# Quit the game
	get_tree().quit()

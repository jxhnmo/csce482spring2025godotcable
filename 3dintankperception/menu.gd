extends Control

var option_button
var selected_map = "res://original.tscn"

func _ready():

	option_button = OptionButton.new()

	$VBoxContainer.add_child(option_button)
	
	option_button.position = Vector2(100, 100)
	option_button.custom_minimum_size = Vector2(200, 50)


	option_button.add_item("Map Original")
	option_button.add_item("Map Squares")
	option_button.add_item("Map Lumpy")

	option_button.connect("item_selected", Callable(self, "_on_OptionButton_item_selected"))

	if not $VBoxContainer/StartButton.pressed.is_connected(Callable(self, "_on_StartButton_pressed")):
		$VBoxContainer/StartButton.pressed.connect(_on_StartButton_pressed)


func _on_OptionButton_item_selected(index):
	var selected_text = option_button.get_item_text(index)
	print("Selected: " + selected_text)
	match selected_text:
		"Map Original":
			selected_map = "res://original.tscn"
		"Map Squares":
			selected_map = "res://squares.tscn"
		"Map Lumpy":
			selected_map = "res://lumpy.tscn"
		_:
			selected_map = "res://original.tscn"  


func _on_StartButton_pressed():
	# Your existing start button logic
	print("Loading map: " + selected_map)
	var result = get_tree().change_scene_to_file(selected_map)
	if result != OK:
		print("Failed to change scene. Error code: ", result)
	else:
		print("Scene change initiated successfully")

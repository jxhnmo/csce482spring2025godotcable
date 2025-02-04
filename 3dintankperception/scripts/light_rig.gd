extends Node3D

var lights_on = true

@onready var top_front = $TopFront
@onready var top_back = $TopBack
@onready var top_right = $TopRight
@onready var top_left = $TopLeft
@onready var headlights = $HeadLights

# Called when the node is ready
func _ready() -> void:
	toggle_lights(lights_on)
	headlights.visible = false

# Function to toggle visibility of all lights
func toggle_lights(is_on: bool) -> void:
	for child in get_children():
		if child is Light3D:
			child.visible = is_on

# Toggle individual lights
func toggle_light(light_node: Node) -> void:
	if light_node is Light3D:
		light_node.visible = !light_node.visible

# Handle input for toggling lights
func _input(event: InputEvent) -> void:
	# Toggle all lights with the "L" key
	if event.is_action_pressed("toggle_lights"):
		lights_on = !lights_on
		toggle_lights(lights_on)
	
	# Toggle individual lights with number keys
	if event.is_action_pressed("toggle_light_1"):
		toggle_light(top_front)
	elif event.is_action_pressed("toggle_light_2"):
		toggle_light(top_back)
	elif event.is_action_pressed("toggle_light_3"):
		toggle_light(top_right)
	elif event.is_action_pressed("toggle_light_4"):
		toggle_light(top_left)
	elif event.is_action_pressed("toggle_light_5"):
		toggle_light(headlights)

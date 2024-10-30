extends VehicleBody3D

var speed = 3
var left
var right
var center_of_mass_marker: MeshInstance3D

func _ready():
	#pass
	 # Create a small sphere mesh for the center of mass marker
	center_of_mass_marker = MeshInstance3D.new()
	center_of_mass_marker.mesh = SphereMesh.new()
	center_of_mass_marker.mesh.radius = 0.1  # Adjust for visibility
	center_of_mass_marker.material_override = StandardMaterial3D.new()
	center_of_mass_marker.material_override.albedo_color = Color(1, 0, 0)  # Set color to red

	# Add marker as a child of the VehicleBody3D
	add_child(center_of_mass_marker)

func _process(delta):
	# Get the current center of mass position
	var com_position = get_center_of_mass()
	
	# Update the position of the center of mass marker in local space
	center_of_mass_marker.transform.origin = com_position
	
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

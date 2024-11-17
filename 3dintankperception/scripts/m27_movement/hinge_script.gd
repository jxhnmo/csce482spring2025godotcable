extends JoltHingeJoint3D

var speed = 0.3
var angle_data = 0

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass

func _process(delta: float) -> void:
	# Get the bodies connected to the joint
	var body_a = get_parent_node_3d()
	var body_b = get_node("../m27_arm")
	
	# Calculate the relative angle between body_a and body_b
	var relative_angle = get_relative_angle(body_a, body_b)
	angle_data = -rad_to_deg(relative_angle)

func _physics_process(delta: float) -> void:
	
	# Adjust velocity based on input
	if Input.is_action_pressed("arm_down"):
		motor_target_velocity = -speed
	elif Input.is_action_pressed("arm_up"):
		motor_target_velocity = speed
	else:
		motor_target_velocity = 0

# Function to calculate the relative angle between two bodies
func get_relative_angle(body_a: Node3D, body_b: Node3D) -> float:
	# Get the world transform (rotation) of both bodies
	var rotation_a = body_a.global_transform.basis
	var rotation_b = body_b.global_transform.basis
	
	# Calculate the relative rotation matrix between the two bodies
	var relative_rotation = rotation_a.transposed() * rotation_b

	# Convert the relative rotation matrix to an angle (using the yaw or pitch, depending on axis of rotation)
	var relative_angle = relative_rotation.get_euler().x  # Use .y if rotating around the Y axis, change as needed

	return relative_angle

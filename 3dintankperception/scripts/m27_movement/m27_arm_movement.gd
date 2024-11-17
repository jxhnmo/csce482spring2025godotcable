extends RigidBody3D

# Variables to control the speed of the arm movement and limits
var arm_speed = 1 # Speed factor for controlling the arm
var upper_limit# Upper limit for movement
var lower_limit# Lower limit for movement

# Exported variables to assign the joint and nodes' paths in the editor
@export var joint_node_path: NodePath
@export var vehicle_body_path: NodePath
var hinge_joint: JoltHingeJoint3D

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	
	# Ensure the joint_node_path is valid
	if joint_node_path:
		hinge_joint = get_node(joint_node_path) as JoltHingeJoint3D

	# Ensure the vehicle body path is valid
	if hinge_joint:
		# Set the NodePaths for node_a and node_b
		hinge_joint.node_a = vehicle_body_path # Assign the NodePath of VehicleBody3D
		hinge_joint.node_b = get_path() # Assign the current RigidBody3D's path
		hinge_joint.use_limits = true
		hinge_joint.upper_limit = upper_limit
		hinge_joint.lower_limit = lower_limit

	# Disable gravity for the body so it doesn't float away
	gravity_scale = 0

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	var velocity = Vector3(0, 0, 0)

	# Adjust velocity based on input
	if Input.is_action_pressed("arm_down"):
		velocity.y -= arm_speed
	elif Input.is_action_pressed("arm_up"):
		velocity.y += arm_speed
	# Set the linear velocity to move the arm along the Y-axis
	linear_velocity = velocity

	# Ensure the arm stops moving once the input is released
	if !Input.is_action_pressed("arm_down") and !Input.is_action_pressed("arm_up"):
		linear_velocity = Vector3.ZERO

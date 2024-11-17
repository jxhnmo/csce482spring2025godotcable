extends JoltHingeJoint3D

var speed = 0.3

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	#motor_max_torque = 0.5
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func _physics_process(delta: float) -> void:
	
	# Adjust velocity based on input
	if Input.is_action_pressed("arm_down"):
		motor_target_velocity = -speed
	elif Input.is_action_pressed("arm_up"):
		motor_target_velocity = speed
	else:
		motor_target_velocity = 0

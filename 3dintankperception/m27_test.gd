extends VehicleBody3D

var power = 500
var left
var right

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func _physics_process(delta: float) -> void:
	# get keyboard
	left = int((Input.get_action_raw_strength("left_tread_forward")
		- Input.get_action_raw_strength("left_tread_backward")) * 10)
	right = int((Input.get_action_raw_strength("right_tread_forward")
		- Input.get_action_raw_strength("right_tread_backward")) * 10)
	
	# handle only one input
	if not left and right:
		$left_middle.engine_force = right * power
	elif left and not right:
		$right_middle.engine_force = left * power
	else:
		$left_middle.engine_force = left * power
		$right_middle.engine_force = right * power
		
		#$LeftWheelForward.engine_force = left * speed
		#$RightWheelForward.engine_force = right * speed
		#
		#$LeftWheelBack.engine_force = left * speed
		#$RightWheelBack.engine_force = right * speed
	
	print($left_middle.engine_force, $right_middle.engine_force)

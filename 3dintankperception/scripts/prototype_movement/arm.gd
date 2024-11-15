extends CharacterBody3D

var arm_speed = 0.5

func _ready() -> void:
	pass
	
func _process(delta: float) -> void:
	var elevation = 0
	if Input.is_action_pressed("arm_down"):
		if rotation_degrees.x > 0:
			elevation -= 1
	if Input.is_action_pressed("arm_up"):
		if rotation_degrees.x < 30:
			elevation += 1
	
	#print(elevation * arm_speed)
	rotation_degrees.x += elevation * arm_speed

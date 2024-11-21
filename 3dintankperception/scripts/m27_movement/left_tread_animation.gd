extends Path3D

var time = 0
var speed_div = 5
var speed = 0
var offset = 0.72

@onready var links = $".".get_children()
@onready var tank = get_parent().get_parent()

func _process(delta):
	if tank and tank.has_method("get") and tank.get("left") != null:
		speed = -tank.left / speed_div
	else:
		speed = 0 
	
	time += delta
	var count = 0
	for index in links:
		index.progress = speed * time + (offset * count)
		count += 1

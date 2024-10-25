extends Node3D

var time = 0
var speed = 2
var offset = 0.7

@onready var links = $Path3D.get_children()

func _process(delta):
	time += delta
	
	var count = 0
	for index in links:
		index.progress = speed + time + (offset * count)
		
		count += 1

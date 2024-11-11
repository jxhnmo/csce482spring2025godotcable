extends Control 


signal help_menu_closed

func close_menu():
	queue_free()
	
func _on_CloseButton_pressed():
	emit_signal("help_menu_closed")
	close_menu()
	
func _ready():
	anchor_left = 0
	anchor_top = 0
	anchor_right = 1
	anchor_bottom = 1
	offset_left = 0
	offset_top = 0
	offset_right = 0
	offset_bottom = 0

	$Label/VBoxContainer/CloseButton.pressed.connect(_on_CloseButton_pressed)
	
func _unhandled_input(event):
	if event.is_action_pressed("ui_cancel"): 
		close_menu()

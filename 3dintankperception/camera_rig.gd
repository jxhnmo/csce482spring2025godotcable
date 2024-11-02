extends Node3D

@export var vehicle: Node3D  # Reference to the vehicle node

# Smoothing factor for position interpolation (adjust to preference)
@export var position_smoothing: float = 0.1

# Adjust the height offset for the camera
@export var height_offset: float = 3.0

# Adjust the distance behind the vehicle for the camera
@export var follow_distance: float = 6.0

func _process(delta):
	if vehicle:
		# Target position behind the vehicle
		var target_position = vehicle.global_transform.origin
		target_position -= vehicle.global_transform.basis.z * follow_distance
		target_position.y += height_offset

		# Smoothly move the CameraRig to the target position using lerp
		global_transform.origin = global_transform.origin.lerp(target_position, position_smoothing)

		# Stabilize rotation to match only the Y-axis (yaw) of the vehicle
		var current_rotation = global_transform.basis.get_euler()
		var vehicle_rotation = vehicle.global_transform.basis.get_euler()

		current_rotation.y = vehicle_rotation.y  # Copy only the yaw rotation
		global_transform.basis = Basis(current_rotation)

class_name FmodGdScriptBridge
extends Node

@export
var SteamAudioBridge : FmodSteamAudioBridge

func event_created(event):
	if SteamAudioBridge != null:
		SteamAudioBridge.FMODEventCreated(event.get_event_pointer())
	
func event_removed(event):
	if SteamAudioBridge != null:
		SteamAudioBridge.FMODEventRemoved(event.get_event_pointer())

func get_buffer_size():
	return FmodServer.get_system_dsp_buffer_length()

static func get_param(param_name : String) -> float:
	return FmodServer.get_global_parameter_by_name(param_name)
	
static func set_param(param_name : String, val : float):
	FmodServer.set_global_parameter_by_name(param_name, val)

func _ready() -> void:
	FmodServer.add_event_created_callback(event_created)
	FmodServer.add_event_removed_callback(event_removed)

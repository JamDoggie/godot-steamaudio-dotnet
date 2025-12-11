@tool
extends EditorPlugin

var gizmo_plugin
var baker_gizmo_plugin

var toolbar_baker : Control
var bake_gui : Control

func _enable_plugin() -> void:
	pass

func _enter_tree():
	# Gizmos
	gizmo_plugin = preload("res://addons/steam-audio-cs-plugin/event_emitter_gizmo.gd").new()
	baker_gizmo_plugin = preload("res://addons/steam-audio-cs-plugin/steamaudio_bake_gizmo.gd").new()
	
	add_node_3d_gizmo_plugin(gizmo_plugin)
	add_node_3d_gizmo_plugin(baker_gizmo_plugin)
	
	# Toolbars
	toolbar_baker = preload("res://addons/steam-audio-cs-plugin/toolbar/steamaudio-bake-tools.tscn").instantiate()
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_MENU, toolbar_baker)
	
	# Baking GUI
	bake_gui = preload("res://addons/steam-audio-cs-plugin/toolbar/steamaudio-bake-popup.tscn").instantiate()
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_SIDE_RIGHT, bake_gui)
	
	toolbar_baker.visible = false
	
	EditorInterface.get_selection().selection_changed.connect(on_selection_changed)
	
func on_selection_changed():
	var selected_nodes = EditorInterface.get_selection().get_selected_nodes()
	
	var bake_visible = false
	
	for node in selected_nodes:
		if node is SteamAudioBaker:
			bake_visible = true
			break
			
	toolbar_baker.visible = bake_visible
	
func _exit_tree():
	remove_node_3d_gizmo_plugin(gizmo_plugin)
	remove_node_3d_gizmo_plugin(baker_gizmo_plugin)
	
	remove_control_from_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_MENU, toolbar_baker)
	toolbar_baker.free()
	
func _has_main_screen() -> bool:
	return false

@tool
extends Button

func _pressed() -> void:
	var baker : SteamAudioBaker = null
	
	for node : Node in EditorInterface.get_selection().get_selected_nodes():
		if node is SteamAudioBaker:
			baker = node as SteamAudioBaker
			break
			
	if baker == null:
		printerr("Couldn't find baker in selection!")
		return
		
	baker.Bake()

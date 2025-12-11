@tool
extends Node
class_name SteamAudioBakePopup

@export
var top_panel : Control
@export
var task_progress : ProgressBar
@export
var general_progress : ProgressBar
@export
var bake_status_label : RichTextLabel
@export
var anim_player : AnimationPlayer
@export
var cancel_close_button : Button

static var bake_popup : SteamAudioBakePopup = null

static func get_singleton():
	return bake_popup

func _ready() -> void:
	bake_popup = self
	
	if top_panel != null:
		top_panel.visible = false
		
	top_panel.focus_entered.connect(on_panel_focused)
	top_panel.focus_exited.connect(on_panel_unfocused)
	
	if cancel_close_button != null:
		cancel_close_button.pressed.connect(cancel_close_pressed)
		
func cancel_close_pressed():
	var currentBaker : SteamAudioBaker = SteamAudioBaker.GetSingleton()
	
	if currentBaker == null:
		anim_player.play("fade_out_full")
		return
	
	if (currentBaker.IsBakeRunning()):
		currentBaker.CancelBake()
	else:
		anim_player.play("fade_out_full")

func on_panel_focused():
	if anim_player != null:
		anim_player.play("fade_half_in")

func on_panel_unfocused():
	if anim_player != null:
		anim_player.play("fade_out_half")

func set_popup_visible(visible : bool):
	if top_panel != null:
		top_panel.visible = visible
		call_deferred("focus_panel")
		
		if visible and anim_player != null:
			anim_player.play("fade_half_in")

func focus_panel():
	top_panel.grab_focus()

func set_task_progress(progress : float):
	if task_progress != null:
		task_progress.value = progress

func set_general_progress(progress : float):
	if general_progress != null:
		general_progress.value = progress

func set_progress_label(str : String):
	if bake_status_label != null:
		bake_status_label.text = str

func set_button_label(str : String):
	if cancel_close_button != null:
		cancel_close_button.text = str

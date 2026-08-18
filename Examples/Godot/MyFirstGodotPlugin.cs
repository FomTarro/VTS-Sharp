using System;
using Godot;
using VTS.Core;

namespace VTS.Godot.Examples
{
	[GlobalClass]
	public partial class MyFirstGodotPlugin : GodotVTSPlugin
	{
		// Called when the node enters the scene tree for the first time.

		[Export]
		private TextureRect _statusLight;
		[Export]
		private Button _connectButton;
		[Export]
		private Button _actionButton;

		public override void _Ready()
		{
			_statusLight.SelfModulate = Colors.Gray;
			_connectButton.Pressed += Connect;
			_actionButton.Pressed += () =>
			{
				if (IsAuthenticated)
				{
					ArtMeshMatcher matcher = new()
					{
						tintAll = true
					};
					TintArtMesh(
						Colors.LightPink,
						0.0f,
						matcher,
						(r) => this.Logger.Log(this.JsonUtility.ToJson(r)),
						(e) => this.Logger.LogError(e.data.message)
					);
				}
				else
				{
					this.Logger.LogWarning("Plugin is not authenticated!");
				}
			};
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.

		public override void _Process(double delta)
		{

		}

		public void Connect()
		{
			_statusLight.SelfModulate = Colors.Yellow;
			Initialize(
				() =>
				{
					this.Logger.Log("Connected!");
					_statusLight.SelfModulate = Colors.Green;
				},
				() =>
				{
					this.Logger.Log("Disconnected!");
					_statusLight.SelfModulate = Colors.Gray;
				},
				(error) =>
				{
					this.Logger.LogError($"Error with connection: {error.data.message}");
					_statusLight.SelfModulate = Colors.Red;
				}
			);
			OS.ShellOpen(ProjectSettings.GlobalizePath("user://"));
		}
	}

}

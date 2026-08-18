using Godot;
using VTS.Core;

namespace VTS.Godot.Examples
{
	[GlobalClass]
	public partial class MyFirstGodotPlugin : GodotVTSPlugin
	{
		[Export]
		private TextureRect _statusLight;
		[Export]
		private SpinBox _portInput;
		[Export]
		private Button _connectButton;
		[Export]
		private Button _actionButton;

		// used to block saving while a load is in progress
		private bool _loading = false;

		private const string SETTINGS_PATH = "user://settings.json";

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			LoadSettings();
			_statusLight.SelfModulate = Colors.Gray;
			_portInput.ValueChanged += (port) =>
			{
				SetPort((int)port);
				SaveSettings();
			};
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
			SaveSettings();
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

		public void SaveSettings()
		{
			if (!_loading)
			{
				using var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
				if (file != null)
				{
					file.StoreString(this.JsonUtility.ToJson(new MyFirstPluginSettingsData()
					{
						version = ProjectSettings.GetSetting("application/config/version").AsString(),
						port = GetPort()

					}));
					this.Logger.Log("Settings file written successfully!");
				}
				else
				{
					this.Logger.LogError($"Failed to open settings file at {SETTINGS_PATH} - {FileAccess.GetOpenError()}");
				}
			}
		}

		public void LoadSettings()
		{
			_loading = true;
			if (!FileAccess.FileExists(SETTINGS_PATH))
			{
				this.Logger.LogError($"Settings file does not exist at {SETTINGS_PATH}");
			}
			else
			{
				using var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Read);
				if (file != null)
				{
					string content = file.GetAsText();
					this.Logger.Log("Settings file read successfully!");
					MyFirstPluginSettingsData settings = this.JsonUtility.FromJson<MyFirstPluginSettingsData>(content);
					_portInput.Value = settings.port;
				}
			}
			_loading = false;
		}
	}

	[System.Serializable]
	public class MyFirstPluginSettingsData
	{
		public string version;
		public int port = 8001;
	}

}

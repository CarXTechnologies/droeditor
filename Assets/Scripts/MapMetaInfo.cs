using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TrackLayout.Data;
using UnityEditor;
using UnityEngine;

public class MapMetaInfo : MonoBehaviour
{
	public ConfigNode[] configs;

	[Button("Fill from resources")]
	void FillFromResources()
	{
		var path = Path.DirectorySeparatorChar.ToString();
		var cfgs = Resources.LoadAll<TrackLayoutData>(path);
		var cfgsOut = new List<ConfigNode>();
		foreach(var itr in cfgs)
		{
			var mode = AutoDetectMode(itr.name);
			cfgsOut.Add(new ConfigNode() { layout = itr as TrackLayoutData, mode = mode });
		}
		configs = cfgsOut.ToArray();
	}

	public RaceMode AutoDetectMode(string configName)
	{
		if (string.IsNullOrEmpty(configName))
		{
			return RaceMode.Default;
		}

		configName = configName.ToLower();
		foreach(var itr in Enum.GetValues(typeof(RaceMode)))
		{
			var sub = ((RaceMode)itr).ToString().ToLower();
			if (configName.Contains(sub))
			{
				return (RaceMode)itr;
			}
		}

		return RaceMode.Default;
	}

	[Button("Save to XML")]
    void SaveToXML()
    {
		if (!ValidateConfigList())
		{
			return;
		}

		System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new VectorConverter());
		settings.Converters.Add(new QuaternionConverter());
		settings.Converters.Add(new Matrix4x4Converter());
		settings.Converters.Add(new ColorConverter());
		settings.Converters.Add(new ResolutionConverter());
		settings.Converters.Add(new HashSetConverter());

		var outputPath = Application.dataPath + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar;
		using (var outputStream = File.CreateText(outputPath + "map.xml"))
		{
			var xmlSettings = new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "	",
			};

			using (var writer = XmlWriter.Create(outputStream, xmlSettings))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("list");
				writer.WriteAttributeString("id", "0");
				var files = "";
				for (int i = 0; i < transform.childCount; ++i)
				{
					var itr = transform.GetChild(i);
					var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(itr);
					if (!string.IsNullOrEmpty(path))
					{
						files += (new FileInfo(path)).Name;
						if(i != transform.childCount - 1)
						{
							files += ",";
						}
					}
				}
				writer.WriteAttributeString("files", files);

				foreach (var itr in configs)
				{
					writer.WriteStartElement("cfg");
					var name = itr.GetName();
					var json = JsonConvert.SerializeObject(itr.layout, Newtonsoft.Json.Formatting.None, settings);
					File.WriteAllText(outputPath + name + ".cfg", json);

					writer.WriteAttributeString("name", name);
					writer.WriteAttributeString("json", name + ".cfg");
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}
		Debug.Log("Saved to " + outputPath);
	}

	private bool ValidateConfigList()
	{
		var sortedConfigs = new Dictionary<RaceMode, List<ConfigNode>>();
		List<ConfigNode> configList;
		foreach (var itr in configs)
		{
			if (!sortedConfigs.TryGetValue(itr.mode, out configList))
			{
				configList = new List<ConfigNode>();
				sortedConfigs.Add(itr.mode, configList);
			}

			configList.Add(itr);
		}

		foreach (var itr in sortedConfigs)
		{
			for (int i = 0; i < itr.Value.Count; ++i)
			{
				itr.Value[i].letter = m_letters[i].ToString();
			}
		}

		if (!sortedConfigs.TryGetValue(RaceMode.Default, out configList))
		{
			Debug.LogError("Map should contain Default config");
			return false;
		}

		if (configList.Count != 1)
		{
			Debug.LogError("Map should contain exactly one Default config");
			return false;
		}

		return true;
	}

	private string m_letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	[System.Serializable]
	public class ConfigNode
	{
		public TrackLayoutData layout;
		public RaceMode mode;

		[NonSerialized]
		public string letter;

		public string GetName()
		{
			return mode == RaceMode.Default ? "Default" : mode.ToString() + " " + letter;
		}
	}

	public enum RaceMode
	{
		Default,
		Generic,
		Drift,
		Time,
		XDS,
		Tandem,
		Clipping,
	}
}

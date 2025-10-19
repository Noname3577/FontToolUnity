using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;

// BepInEx IL2CPP plugin that adds Thai font support to Unity games
// Loads .ttf font from plugin folder - easy to customize!

namespace ThaiFontMod
{
    [BepInPlugin("thai.font.mod", "Thai Font Fallback Mod", "1.0.0")]
    public class ThaiFontModPlugin : BasePlugin
    {
        private TMP_FontAsset _thaiFont;

        public override void Load()
        {
            Log.LogInfo("Thai Font Mod loading...");

            // Load Thai font from plugin folder
            TryLoadEmbeddedFont();

            // Apply fallback font
            ApplyFallback();
            
            Log.LogInfo("Thai Font Mod loaded successfully");
        }

        private void TryLoadEmbeddedFont()
        {
            try
            {
                Log.LogInfo("Searching for Thai fonts...");
                
                // Try to load font from plugin folder first
                _thaiFont = TryLoadFromPluginFolder();
                if (_thaiFont != null)
                {
                    Log.LogInfo("Successfully loaded font from plugin folder");
                    return;
                }
                
                // List of Thai-capable fonts to try from system
                string[] fontCandidates = { 
                    "Itim",
                    "Itim-Regular",
                    "Sarabun",
                    "Noto Sans Thai",
                    "Leelawadee UI",
                    "Leelawadee",
                    "Tahoma",
                    "Cordia New",
                    "Browallia New",
                    "Angsana New",
                    "Arial Unicode MS"
                };

                // Try each system font
                foreach (var fontName in fontCandidates)
                {
                    _thaiFont = TryCreateFontAsset(fontName);
                    if (_thaiFont != null)
                    {
                        Log.LogInfo($"Successfully loaded Thai font: {fontName}");
                        return;
                    }
                }

                Log.LogError("No Thai-capable font found.");
                Log.LogError($"Please place a .ttf file in: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to load Thai font: {ex.Message}");
            }
        }

        private TMP_FontAsset TryLoadFromPluginFolder()
        {
            try
            {
                var pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Log.LogInfo($"Plugin folder: {pluginFolder}");
                
                string[] fontFiles = { 
                    "Itim-Regular.ttf",
                    "Sarabun-Regular.ttf", 
                    "NotoSansThai-Regular.ttf",
                    "font.ttf",
                    "thai.ttf"
                };

                foreach (var fontFile in fontFiles)
                {
                    var fontPath = Path.Combine(pluginFolder, fontFile);
                    
                    if (File.Exists(fontPath))
                    {
                        Log.LogInfo($"Found font file: {fontFile}");
                        
                        try
                        {
                            var fontData = File.ReadAllBytes(fontPath);
                            Log.LogInfo($"Loaded font file, size: {fontData.Length} bytes");

                            var tempPath = Path.Combine(Path.GetTempPath(), $"ThaiFontMod_{Path.GetFileNameWithoutExtension(fontFile)}_{System.DateTime.Now.Ticks}.ttf");
                            File.WriteAllBytes(tempPath, fontData);

                            TMP_FontAsset tmpFont = null;
                            
                            try
                            {
                                // Try loading methods
                                var font = new Font("file:///" + tempPath.Replace('\\', '/'));
                                tmpFont = TMP_FontAsset.CreateFontAsset(font);
                                
                                if (tmpFont == null)
                                {
                                    font = new Font(tempPath);
                                    tmpFont = TMP_FontAsset.CreateFontAsset(font);
                                }
                                
                                if (tmpFont == null)
                                {
                                    font = new Font(Path.GetFileNameWithoutExtension(fontFile));
                                    tmpFont = TMP_FontAsset.CreateFontAsset(font);
                                }

                                if (tmpFont != null)
                                {
                                    tmpFont.name = $"{Path.GetFileNameWithoutExtension(fontFile)} (External)";
                                    tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                                    tmpFont.atlasPadding = 5;
                                    
                                    Log.LogInfo($"? Successfully loaded font from: {fontFile}");
                                    return tmpFont;
                                }
                            }
                            finally
                            {
                                try { File.Delete(tempPath); } catch { }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.LogWarning($"Failed to load {fontFile}: {ex.Message}");
                        }
                    }
                }
                
                Log.LogInfo("No font files found in plugin folder");
                Log.LogInfo($"Please place a .ttf file in: {pluginFolder}");
                
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to load external font: {ex.Message}");
            }
            return null;
        }

        private TMP_FontAsset TryCreateFontAsset(string fontName)
        {
            try
            {
                var font = new Font(fontName);
                if (font == null) return null;

                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    tmpFont.name = $"{fontName} (System)";
                    tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    tmpFont.atlasPadding = 5;
                    return tmpFont;
                }
            }
            catch { }
            return null;
        }

        private void ApplyFallback()
        {
            if (_thaiFont == null)
            {
                Log.LogWarning("Thai font not available; fallback not applied.");
                return;
            }

            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                Log.LogWarning("TMP_Settings.instance is null.");
                return;
            }

            // Add to global fallback list
            try
            {
                var fallbackProp = typeof(TMP_Settings).GetProperty("fallbackFontAssets");
                if (fallbackProp != null)
                {
                    var list = fallbackProp.GetValue(settings) as Il2CppSystem.Collections.Generic.List<TMP_FontAsset>;
                    if (list != null && !list.Contains(_thaiFont))
                    {
                        list.Add(_thaiFont);
                        Log.LogInfo($"Added '{_thaiFont.name}' to TMP global fallback list.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to add fallback font: {ex.Message}");
            }

            // Set as default font
            try
            {
                var defaultProp = typeof(TMP_Settings).GetProperty("defaultFontAsset");
                if (defaultProp != null && defaultProp.CanWrite)
                {
                    defaultProp.SetValue(settings, _thaiFont);
                    Log.LogInfo($"Set '{_thaiFont.name}' as TMP default font.");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Could not set default font: {ex.Message}");
            }

            Log.LogInfo("Thai font will apply to new text and scenes.");
        }
    }
}

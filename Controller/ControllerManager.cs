using System.Diagnostics;
using System.Text.Json;

namespace OverjoyedVersion3;

/// <summary>
/// Singleton that owns all in-memory Controllers and manages them.
/// </summary>
public static class ControllerManager
{
    public static string ControllerDirectory { get; } =
        Path.Combine(FileSystem.AppDataDirectory, "Controllers");

    public static Dictionary<string, Controller> Controllers { get; private set; } = new();
    public static Controller ActiveController { get; private set; } = new();

    private static bool _initialized;

    public static async Task Initialize()
    {
        if (_initialized)
        {
            Debug.WriteLine("ControllerManager was already initialized!");
            return;
        }

        _initialized = true;

        // Load all the controllers from the ControllerDirectory.
        await LoadAllControllersAsync();
        foreach (var controller in Controllers.Values)
        {
            controller.Initialize();
        }

        if (Controllers.Count > 0)
        {
            ActiveController = Controllers.Values.First();
        }
    }

    /// <summary>
    /// Adds a Controller to the manager.
    /// </summary>
    public static void AddController(Controller controller)
    {
        Controllers[controller.Name] = controller;
    }
    /// <summary>
    /// Removes a Controller from the manager.
    /// </summary>
    public static void RemoveController(string name)
    {
        Controllers.Remove(name);
    }
    /// <summary>
    /// Sets the specified controller as the one currently loaded on the interface.
    /// </summary>
    public static void SetControllerAsActive(Controller controller)
    {
        if (controller == null) return;

        ActiveController = controller;
    }

    /// <summary>
    /// Saves a Controller object to the ControllerDirectory as a JSON file.
    /// </summary>
    public static async Task SaveControllerAsync(string controllerName)
    {
        if (!Controllers.TryGetValue(controllerName, out var controller)) return;
        await WriteControllerAsync(controller, $"{controllerName}.json");
    }
    /// <summary>
    /// Saves a Controller object to the ControllerDirectory as a JSON file.
    /// </summary>
    public static async Task SaveControllerAsync(Controller controller, string fileName)
    {
        await WriteControllerAsync(controller, fileName);
    }

    /// <summary>
    /// Loads all Controller JSON files from the ControllerDirectory.
    /// </summary>
    private static async Task LoadAllControllersAsync()
    {
        Directory.CreateDirectory(ControllerDirectory);

        foreach (var path in Directory.EnumerateFiles(ControllerDirectory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var controller = JsonSerializer.Deserialize<Controller>(json);
                if (controller != null)
                {
                    Controllers[controller.Name] = controller;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load controller '{path}': {ex.Message}");
            }
        }
    }
    private static async Task WriteControllerAsync(Controller controller, string fileName)
    {
        Directory.CreateDirectory(ControllerDirectory);
        var json = JsonSerializer.Serialize(controller, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(ControllerDirectory, fileName), json);
    }
}

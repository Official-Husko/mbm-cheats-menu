using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using MBMScripts;
using UnityEngine;

namespace mbm_cheats_menu
{
    [BepInPlugin("husko.monsterblackmarket.cheats", "Monster Black Market Cheats", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private enum Tab
        {
            MainCheats,
            EventsCheats,
            InteractiveSpots
        }

        private Tab _currentTab = Tab.MainCheats;
        private bool _showMenu;
        private Rect _menuRect = new(20, 20, 630, 240); // Initial position and size of the menu
        
        // Define separate arrays to store activation status for each tab
        private readonly bool[] _mainCheatsActivated = new bool[0];
        private readonly bool[] _interactiveSpotsActivated = new bool[0];
        
        // Default max values
        private string _addGoldAmountText = "0";
        private string _addPixyAmountText = "0";
        private string _addAchievementPointsAmountText = "0";
        private string _addReputationAmountText = "0";
        private string _addSoulAmountText = "0";
        private string _addAllItemsAmountText = "0";
        
        private const string VersionLabel = MyPluginInfo.PLUGIN_VERSION;
        private int _selectedOptionIndex = 0;
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly List<EPlayEventType> _availableEvents = new List<EPlayEventType>();

        // List to store button labels and corresponding actions for the current cheats tab
        private readonly List<(string label, Action action)> _mainCheatsButtonActions = new()
        {
            // Add more buttons and actions here
        };

        // Modify the ghostModeButtonActions list to include a button for Special Cheats
        private readonly List<(string label, Action action)> _specialCheatsButtonActions = new()
        {
            // Add more buttons for Special Cheats here
        };
        
        /// <summary>
        /// Initializes the plugin on Awake event
        /// </summary>
        private void Awake()
        {
            // Log the plugin's version number and successful startup
            Logger.LogInfo($"Plugin mbm-cheats-menu v{VersionLabel} loaded!");

            // Fetch available events
            FetchAvailableEvents();
        }
        
        /// <summary>
        /// Handles toggling the menu on and off with the Insert or F1 key.
        /// </summary>
        private void Update()
        {
            // Toggle menu visibility with Insert or F1 key
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F1))
            {
                _showMenu = !_showMenu;
            }
        }

        /// <summary>
        /// Handles drawing the menu and all of its elements on the screen.
        /// </summary>
        private void OnGUI()
        {
            // Only draw the menu if it's supposed to be shown
            if (!_showMenu) return;
            // Apply dark mode GUI style
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

            // Draw the IMGUI window
            _menuRect = GUI.Window(0, _menuRect, MenuWindow, "----< Cheats Menu >----");

            // Calculate position for version label at bottom left corner
            float versionLabelX = _menuRect.xMin + 10; // 10 pixels from left edge
            float versionLabelY = _menuRect.yMax - 20; // 20 pixels from bottom edge

            // Draw version label at bottom left corner
            GUI.contentColor = new Color(0.5f, 0.5f, 0.5f); // Dark grey silver color
            GUI.Label(new Rect(versionLabelX, versionLabelY, 100, 20), "v" + VersionLabel);

            // Calculate the width of the author label
            float authorLabelWidth = GUI.skin.label.CalcSize(new GUIContent("by Official-Husko")).x + 10; // Add some extra width for padding

            // Calculate position for author label at bottom right corner
            float authorLabelX = _menuRect.xMax - authorLabelWidth; // 10 pixels from right edge
            float authorLabelY = versionLabelY + 2; // Align with version label

            // Draw the author label as a clickable label
            if (GUI.Button(new Rect(authorLabelX, authorLabelY, authorLabelWidth, 20), "<color=cyan>by</color> <color=yellow>Official-Husko</color>", GUIStyle.none))
            {
                // Open a link in the user's browser when the label is clicked
                Application.OpenURL("https://github.com/Official-Husko/Churn-Vector-Cheats");
            }
        }

        /// <summary>
        /// Handles the GUI for the main menu
        /// </summary>
        /// <param name="windowID">The ID of the window</param>
        private void MenuWindow(int windowID)
        {
            // Make the whole window draggable
            GUI.DragWindow(new Rect(0, 0, _menuRect.width, 20));

            // Begin a vertical group for menu elements
            GUILayout.BeginVertical();

            // Draw tabs
            GUILayout.BeginHorizontal();
            // Draw the Main Cheats tab button
            DrawTabButton(Tab.MainCheats, "Main Cheats");
            // Draw the Special Cheats tab button
            DrawTabButton(Tab.EventsCheats, "Event Cheats");
            // Draw Interactive Spots tab button
            DrawTabButton(Tab.InteractiveSpots, "Interactive Spots");
            GUILayout.EndHorizontal();

            // Draw content based on the selected tab
            switch (_currentTab)
            {
                // Draw the Main Cheats tab
                case Tab.MainCheats:
                    DrawMainCheatsTab();
                    break;
                // Draw the Special Cheats tab
                case Tab.EventsCheats:
                    DrawEventsCheatsTab();
                    break;
                // Draw the Interactive Spots tab
                case Tab.InteractiveSpots:
                    DrawInteractiveSpotsTab();
                    break;
            }

            // End the vertical group
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a tab button
        /// </summary>
        /// <param name="tab">The tab to draw</param>
        /// <param name="label">The label to display on the button</param>
        private void DrawTabButton(Tab tab, string label)
        {
            // Change background color based on the selected tab
            GUI.backgroundColor = _currentTab == tab ? Color.white : Color.grey;

            // If the button is clicked, set the current tab to the clicked tab
            if (GUILayout.Button(label))
            {
                _currentTab = tab;
            }
        }
        
        /// <summary>
        /// Gets the activation status array for the currently selected tab
        /// </summary>
        /// <returns>The activation status array for the current tab. If the tab is not recognized, null is returned.</returns>
        private bool[] GetCurrentTabActivationArray()
        {
            switch (_currentTab)
            {
                case Tab.MainCheats:
                    // Return the activation status array for the main cheats tab
                    return _mainCheatsActivated;
                case Tab.EventsCheats:
                    return null;
                case Tab.InteractiveSpots:
                    // Return the activation status array for the interactive spots tab
                    return _interactiveSpotsActivated;
                default:
                    // If the tab is not recognized, return null
                    return null;
            }
        }
        
        /// <summary>
        /// Toggles the activation state of the button at the given index on the currently selected tab.
        /// If the index is not within the range of the activation status array for the current tab, nothing is done.
        /// </summary>
        /// <param name="buttonIndex">The index of the button to toggle activation status for</param>
        private void ToggleButtonActivation(int buttonIndex)
        {
            // Get the activation status array for the current tab. If the tab is not recognized, return.
            bool[] currentTabActivationArray = GetCurrentTabActivationArray();
            if (currentTabActivationArray == null)
            {
                return;
            }

            // If the index is within the range of the activation status array, toggle the activation status
            if (buttonIndex >= 0 && buttonIndex < currentTabActivationArray.Length)
            {
                currentTabActivationArray[buttonIndex] = !currentTabActivationArray[buttonIndex];
            }
        }

        /// <summary>
        /// Method to draw content for the Main Cheats tab
        /// </summary>
        private void DrawMainCheatsTab()
        {
            GUILayout.BeginVertical();

            // Draw buttons from the list
            for (int i = 0; i < _mainCheatsButtonActions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                DrawActivationDot(_mainCheatsActivated[i]); // Draw activation dot based on activation status
                
                // Draws a button for each cheat with the label, 
                // activation status, and invokes the action associated 
                // with the button when pressed
                if (GUILayout.Button(_mainCheatsButtonActions[i].label))
                {
                    ToggleButtonActivation(i); // Toggle activation status
                    _mainCheatsButtonActions[i].action.Invoke(); // Invoke the action associated with the button
                }
                GUILayout.EndHorizontal();
            }
            
            // Draw Gold option
            DrawAddGoldOption();
            
            // Draw Pixy option
            DrawAddPixyOption();
            
            // Draw Achievement Points option
            DrawAddAchievementPointsOption();
            
            // Draw Reputation option
            DrawAddReputationOption();
            
            // Draw Soul Option
            DrawAddSoulOption();
            
            // Draw ADd all items option
            DrawAddAllItemsOption();
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the Special Cheats tab in the mod's UI
        /// </summary>
        private void DrawEventsCheatsTab()
        {
            // Begin vertical layout for the tab
            GUILayout.BeginVertical();

            // Iterate through the list of special cheat buttons
            for (int i = 0; i < _specialCheatsButtonActions.Count; i++)
            {
                // Begin horizontal layout for the button row
                GUILayout.BeginHorizontal();

                // Draw a button for the special cheat
                if (GUILayout.Button(_specialCheatsButtonActions[i].label))
                {
                    // Toggle the activation status of the button
                    ToggleButtonActivation(i);

                    // Invoke the action associated with the button
                    _specialCheatsButtonActions[i].action.Invoke();
                }

                // End the horizontal layout for the button row
                GUILayout.EndHorizontal();
            }
            
            // Draw Event option
            DrawEventsOption();
            
            // End the vertical layout for the tab
            GUILayout.EndVertical();
        }
        
        // Draws the Interactive Spots tab in the mod's UI
        private void DrawInteractiveSpotsTab()
        {
        }

        /// <summary>
        /// Draws a small dot with a green color if the activation status is true, and red if it's false.
        /// This method uses the current tab activation status array to determine the dot color.
        /// </summary>
        /// <param name="activated">The activation status to determine the dot color.</param>
        private void DrawActivationDot(bool activated)
        {
            GetCurrentTabActivationArray(); // Consider current tab activation status array
            GUILayout.Space(10); // Add some space to center the dot vertically
            Color dotColor = activated ? Color.green : Color.red; // Determine dot color based on activation status
            GUIStyle dotStyle = new GUIStyle(GUI.skin.label); // Create a new GUIStyle for the dot label
            dotStyle.normal.textColor = dotColor; // Set the color of the dot label
            GUILayout.Label("●", dotStyle, GUILayout.Width(20), GUILayout.Height(20)); // Draw dot with the specified style
        }
        
        private void DrawBlueDot()
        {
            GUILayout.Space(10); // Add some space to center the dot vertically
            Color blueDotColor = new Color(0.0f, 0.5f, 1.0f); // blue because nice
            GUIStyle dotStyle = new GUIStyle(GUI.skin.label); // Create a new GUIStyle for the dot label
            dotStyle.normal.textColor = blueDotColor; // Set the color of the dot label
            GUILayout.Label("●", dotStyle, GUILayout.Width(20), GUILayout.Height(20)); // Draw dot with the specified style
        }
        
        /// <summary>
        /// Draws the Plushy Uses option in the mod menu
        /// </summary>
        private void DrawAddGoldOption()
        {
            // Begin horizontal layout for the Plushy Uses option
            GUILayout.BeginHorizontal();
            
            // Draw Blue Dot
            DrawBlueDot();
            
            // Add a label for the text field
            GUILayout.Label("Add Gold:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addGoldAmountText = GUILayout.TextField(_addGoldAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            if (int.TryParse(_addGoldAmountText, out var addGoldAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addGoldAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.Gold += addGoldAmountInt;
                    }
                }
            }

            // End horizontal layout for the Plushy Uses option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Pixy option in the mod menu
        /// </summary>
        private void DrawAddPixyOption()
        {
            // Begin horizontal layout for the Add Pixy option
            GUILayout.BeginHorizontal();
            
            // Draw Blue Dot
            DrawBlueDot();

            // Add a label for the text field
            GUILayout.Label("Add Pixy:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addPixyAmountText = GUILayout.TextField(_addPixyAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            if (int.TryParse(_addPixyAmountText, out var addPixyAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addPixyAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.FloraPixyCount += addPixyAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Pixy option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Pixy option in the mod menu
        /// </summary>
        private void DrawAddAchievementPointsOption()
        {
            // Begin horizontal layout for the Add Pixy option
            GUILayout.BeginHorizontal();

            // Draw Blue Dot
            DrawBlueDot();
            
            // Add a label for the text field
            GUILayout.Label("Add Achievement Points:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addAchievementPointsAmountText = GUILayout.TextField(_addAchievementPointsAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            if (int.TryParse(_addAchievementPointsAmountText, out var addAchievementPointsAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addAchievementPointsAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.AchievementPoint += addAchievementPointsAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Pixy option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Reputation option in the mod menu
        /// </summary>
        private void DrawAddReputationOption()
        {
            // Begin horizontal layout for the Add Reputation option
            GUILayout.BeginHorizontal();

            // Draw Blue Dot
            DrawBlueDot();
            
            // Add a label for the text field
            GUILayout.Label("Add Reputation:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addReputationAmountText = GUILayout.TextField(_addReputationAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addReputationAmountInt;
            if (int.TryParse(_addReputationAmountText, out addReputationAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addReputationAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        // Add Reputation points to the player's data
                        GameManager.Instance.PlayerData.Reputation += addReputationAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Reputation option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Soul option in the mod menu
        /// </summary>
        private void DrawAddSoulOption()
        {
            // Begin horizontal layout for the Add Soul option
            GUILayout.BeginHorizontal();

            // Draw Blue Dot
            DrawBlueDot();
            
            // Add a label for the text field
            GUILayout.Label("Add Soul:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addSoulAmountText = GUILayout.TextField(_addSoulAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addSoulAmountInt;
            if (int.TryParse(_addSoulAmountText, out addSoulAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addSoulAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        // Add Soul points to the player's data
                        GameManager.Instance.PlayerData.Soul += addSoulAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Soul option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add All Items option in the mod menu
        /// </summary>
        private void DrawAddAllItemsOption()
        {
            // Begin horizontal layout for the Add All Items option
            GUILayout.BeginHorizontal();

            // Draw Blue Dot
            DrawBlueDot();
            
            // Add a label for the text field
            GUILayout.Label("Add All Items:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            _addAllItemsAmountText = GUILayout.TextField(_addAllItemsAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addAllItemsAmountInt;
            if (int.TryParse(_addAllItemsAmountText, out addAllItemsAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addAllItemsAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_HumanDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_ElfDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_DwarfDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_NekoDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_InuDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_UsagiDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_HitsujiDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_DragonianDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_GoblinDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_OrcDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_WerewolfDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_MinotaurDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_SalamanderDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_OriginDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.BodyFluid, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.VenerealDiseaseDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.HumanPheromone, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.ElfManaEngine, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.DawrfHeart, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.InuMammaryGlandDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.NecoOvarianDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.UsagiWombDna, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.HitsujiHorn, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.DragonTailMeat, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.GoblinSemen, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.OrcHeart, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.WerewolfTail, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.MinotaurSkin, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_SalamanderScalePiece, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.TattooRemovalInjection, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.HealthRecoveryInjection, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.MammaryGlandRecoveryInjection, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.VenerealDiseaseRecoveryInjection, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.OvumRecoveryInjection, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_FertilityMedication, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_Aphrodisiac, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_ViOgra, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_CosmeticPill, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_MonsterCosmeticPill, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_SlaveCosmeticPill, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_TraitUpgradePill, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Milk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.LoliMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.HumanMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.ElfMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.DwarfMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.FurryMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.SmallFurryMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.DragonianMilk, ESector.Inventory, new ValueTuple<int, int>(1, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_Sensitivity3000x, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_Washer, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_Condom, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_LoveGel, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                        GameManager.Instance.PlayerData.NewItem(EItemType.Item_TentacleEgg, ESector.Inventory, new ValueTuple<int, int>(0, 0), -1, addAllItemsAmountInt);
                    }
                }
            }
            // End horizontal layout for the Add All Items option
            GUILayout.EndHorizontal();
        }
        
        private void FetchAvailableEvents()
        {
            // Get all enum values from EPlayEventType
            Type enumType = typeof(MBMScripts.EPlayEventType);
            if (enumType.IsEnum)
            {
                Array enumValues = Enum.GetValues(enumType);
                foreach (var value in enumValues)
                {
                    _availableEvents.Add((EPlayEventType)value);
                }
            }
        }
        
        private void DrawEventsOption()
        {
            GUILayout.BeginHorizontal();

            // Draw the dot
            DrawBlueDot();

            // Label for the selection
            GUILayout.Label("Select Event:");

            // Scroll view for the selection
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100), GUILayout.Width(230));
            {
                // Convert availableEvents to string array for dropdownOptions
                string[] dropdownOptions = _availableEvents.ConvertAll<string>(x => x.ToString()).ToArray();

                // Drop-down menu to select an option
                _selectedOptionIndex = GUILayout.SelectionGrid(_selectedOptionIndex, dropdownOptions, 1, GUILayout.Width(200));
            }
            GUILayout.EndScrollView();
            
            if (GUILayout.Button("Execute"))
            {
                // Check if a valid option is selected
                if (_selectedOptionIndex >= 0 && _selectedOptionIndex < _availableEvents.Count)
                {
                    // Get the selected event from availableEvents
                    EPlayEventType selectedEvent = _availableEvents[_selectedOptionIndex];

                    // Add the selected event to PlayData
                    PlayData.Instance.AddPlayEvent(selectedEvent);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
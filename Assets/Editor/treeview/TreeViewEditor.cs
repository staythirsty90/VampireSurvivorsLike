using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using static PowerUpEditor;
using UnityEngine.SearchService;

public class TreeViewEditor : EditorWindow {
    [SerializeField] VisualTreeAsset uxml;

    List<ListEntry> data = new() {
        new ListEntry {
            name = "Something",
            spritename = "question_mark"
        }
    };

    [MenuItem("Window/UI Toolkit/TreeViewEditor")]
    public static void ShowExample() {
        var wnd = GetWindow<TreeViewEditor>();
        wnd.titleContent = new GUIContent("TreeViewEditor");

        // limit window size
        wnd.minSize = new Vector2(450, 200);
        wnd.maxSize = new Vector2(1920, 900);
    }
   
    // Nested interface that can be either a single planet or a group of planets.
    protected interface IPlanetOrGroup {
        public string name {
            get;
        }

        public bool populated {
            get;
        }
    }

    protected class Planet : IPlanetOrGroup {
        public string name {
            get;
        }

        public bool populated {
            get;
        }

        public Planet(string name, bool populated = false) {
            this.name = name;
            this.populated = populated;
        }
    }

    // Nested class that represents a group of planets.
    protected class PlanetGroup : IPlanetOrGroup {
        public string name {
            get;
        }

        public bool populated {
            get {
                var anyPlanetPopulated = false;
                foreach(Planet planet in planets) {
                    anyPlanetPopulated = anyPlanetPopulated || planet.populated;
                }
                return anyPlanetPopulated;
            }
        }

        public readonly IReadOnlyList<Planet> planets;

        public PlanetGroup(string name, IReadOnlyList<Planet> planets) {
            this.name = name;
            this.planets = planets;
        }
    }

    // Data about planets in our solar system.
    protected static readonly List<PlanetGroup> planetGroups = new List<PlanetGroup>
    {
        new PlanetGroup("A Planets", new List<Planet>
        {
            new Planet("Mercury"),
            new Planet("Venus"),
            new Planet("Earth", true),
            new Planet("Mars"),
        }),
        new PlanetGroup("B Planets", new List<Planet>
        {
            new Planet("Jupiter"),
            new Planet("Saturn"),
            new Planet("Uranus"),
            new Planet("Neptune")
        }),
        new PlanetGroup("C Planets", new List<Planet>
        {
            new Planet("Jupiter"),
            new Planet("Saturn"),
            new Planet("Uranus"),
            new Planet("Neptune")
        })
    };

    // Expresses planet data as a list of TreeViewItemData objects. Needed for TreeView and MultiColumnTreeView.
    protected static IList<TreeViewItemData<IPlanetOrGroup>> treeRoots {
        get {
            int id = 0;
            var roots = new List<TreeViewItemData<IPlanetOrGroup>>(planetGroups.Count);
            foreach(var group in planetGroups) {
                var planetsInGroup = new List<TreeViewItemData<IPlanetOrGroup>>(group.planets.Count);
                foreach(var planet in group.planets) {
                    planetsInGroup.Add(new TreeViewItemData<IPlanetOrGroup>(id++, planet));
                }

                //roots.Add(new TreeViewItemData<IPlanetOrGroup>(id++, group, planetsInGroup));
                roots.Add(new TreeViewItemData<IPlanetOrGroup>(id++, group, planetsInGroup));
            }
            return roots;
        }
    }

    public void CreateGUI() {
        var root = rootVisualElement;
        uxml.CloneTree(root);

        var treeview = root.Q<TreeView>();

        treeview.SetRootItems(treeRoots);

        treeview.makeItem = () => new TreeEntry();
        treeview.bindItem = (item, index) => {
            var tv = item as TreeEntry;
            tv.label.text = $"{treeview.GetItemDataForIndex<IPlanetOrGroup>(index).name} {index}";
        };
    }
}

public class TreeEntry : VisualElement {
    public Image image = new Image();
    public Label label = new Label();
    public TreeEntry() {
        Add(image);
        Add(label);
        image.AddToClassList("TreeImage");
        AddToClassList("TreeEntry");
    }
}
using Localization.Editor.TestConfigs;
using Localization.Json;
using Localization.Xml;
using Localization.Yaml;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Localization.Editor.ViewModels
{
    public class TreeNode : INotifyPropertyChanged
    {
        public TreeNode(TreeNode? parent, string name)
        {
            _parent = parent;
            Name = name;
        }

        #region Properties
        public TreeNode? Parent
        {
            get => _parent;
            set
            {
                if (value == _parent) return;

                var prevParent = _parent;
                _parent = value;

                prevParent?.RemoveChild(this);
                _parent?.AddChild(this);

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsRootNode));
            }
        }
        private TreeNode? _parent;
        public bool IsRootNode => Parent == null || this is LanguageVM;
        public bool IsSelected { get; set; } = false;
        public bool IsExpanded { get; set; } = false;
        [AlsoNotifyFor(nameof(HasChildren))]
        public ObservableCollection<TreeNode>? Children { get; set; }
        public bool HasChildren => Children != null;
        public string Name { get; set; }
        [AlsoNotifyFor(nameof(HasValue))]
        public string? Value { get; set; }
        public bool HasValue => Value != null;
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods
        public override string ToString() => Name;
        public T[] GetBranch<T>(Func<TreeNode, T> selector, bool reverse = true, bool includeRootNode = false)
        {
            List<T> branch = new();

            for (TreeNode? node = this; node != null; node = node.Parent)
            {
                if (node.IsRootNode && !includeRootNode) break;
                branch.Add(selector(node));
            }

            if (reverse) branch.Reverse();
            return branch.ToArray();
        }
        public void ForEachBranchNode(Action<TreeNode> action)
        {
            for (TreeNode? node = this; node != null; node = node.Parent)
            {
                action(node);
            }
        }
        public string[] GetPathArray() => GetBranch(n => n.Name);
        public string GetPath(char separator = Loc.PathSeparator) => string.Join(separator, GetPathArray());
        /// <summary>
        /// Searches this node's direct children for a node matching the specified <paramref name="predicate"/>.
        /// </summary>
        public TreeNode? FindChild(Func<TreeNode, bool> predicate)
            => Children?.FirstOrDefault(predicate);
        public TreeNode? FindDepthFirst(Func<TreeNode, bool> predicate)
        {
            if (Children == null) return predicate(this) ? this : null;
            Stack<TreeNode> stack = new();
            stack.EnsureCapacity(1 + Children.Count);
            stack.Push(this);
            while (stack.Count > 0)
            {
                TreeNode current = stack.Pop();

                if (predicate(current)) return current;

                if (current.Children != null)
                {
                    stack.EnsureCapacity(stack.Count + current.Children.Count);
                    for (int i = current.Children.Count; i >= 0; --i)
                    {
                        stack.Push(current.Children[i]);
                    }
                }
            }
            return null;
        }
        public TreeNode? FindBreadthFirst(Func<TreeNode, bool> predicate)
        {
            if (Children == null) return predicate(this) ? this : null;
            Queue<TreeNode> queue = new();
            queue.EnsureCapacity(1 + Children.Count);
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                TreeNode current = queue.Dequeue();

                if (predicate(current)) return current;

                if (current.Children != null)
                {
                    queue.EnsureCapacity(queue.Count + current.Children.Count);
                    for (int i = current.Children.Count; i >= 0; --i)
                    {
                        queue.Enqueue(current.Children[i]);
                    }
                }
            }
            return null;
        }
        public void ForEachDepthFirst(Action<TreeNode> action)
        {
            Stack<TreeNode> stack = new();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                action(current);

                if (current.Children != null)
                {
                    foreach (var child in current.Children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }
        private bool RemoveChild(TreeNode child)
        {
            if (Children != null && Children.Remove(child))
            {
                if (Children.Count == 0)
                    Children = null;
                return true;
            }
            else return false;
        }
        private void AddChild(TreeNode child)
        {
            Children ??= new();
            Children.Add(child);
        }
        #endregion Methods
    }
    public class LanguageVM : TreeNode, INotifyPropertyChanged
    {
        public LanguageVM(string languageName, TranslationDictionary translations) : base(null, languageName)
        {
            BuildTree(this, translations);
        }
        public LanguageVM(string languageName) : this(languageName, new()) { }

        #region Properties
        public string? FilePath { get; set; }
        #endregion Properties

        #region Methods
        private static void BuildTree(TreeNode root, TranslationDictionary translationDictionary)
        {
            if (translationDictionary.Count > 0)
                root.Children = new();
            foreach (var (key, value) in translationDictionary)
            {
                if (key.Length == 0) continue;
                var path = key.Split(Loc.PathSeparator);
                if (path.Length == 0) continue;

                // create the branch
                TreeNode? node = root.Children!.GetOrCreate(n => n.Name.Equals(path[0], StringComparison.Ordinal), () => new(root, path[0]));
                for (int i = 1, i_max = path.Length; i < i_max; ++i)
                {
                    var pathSegment = path[i];

                    if (node.Children == null)
                    {
                        if (node.Value != null)
                            throw new InvalidOperationException($"Did not expect \"{node.Name}\" to have a value ({node.Value})!");

                        node.Children = new();
                        var child = new TreeNode(node, pathSegment);
                        node.Children.Add(child);
                        node = child;
                    }
                    else node = node.Children.GetOrCreate(n => n.Name.Equals(pathSegment, StringComparison.Ordinal), () => new(node, pathSegment));
                }
                if (node.Children != null)
                    throw new InvalidOperationException($"Did not expect \"{node.Name}\" to have children (\"{string.Join("\", \"", node.Children.Select(n => n.Name))}\")!");

                // set the node's value
                node.Value = value;
            }
        }
        #endregion Methods
    }
    public class MainWindowVM : INotifyPropertyChanged
    {
        #region Constructor
        public MainWindowVM()
        {
            Loc.Instance.LanguageAdded += this.Instance_LanguageAdded;
            Loc.Instance.LanguageRemoved += this.Instance_LanguageRemoved;

            // add translation loaders
            var jsonLoader = Loc.Instance.AddTranslationLoader<JsonTranslationLoader>();
            Loc.Instance.AddTranslationLoader<JsonSingleTranslationLoader>();
            Loc.Instance.AddTranslationLoader<YamlTranslationLoader>();
            Loc.Instance.AddTranslationLoader<YamlSingleTranslationLoader>();
            Loc.Instance.AddTranslationLoader<XmlTranslationLoader>();

            // add debug languages
            Loc.Instance.AddLanguage(jsonLoader.Deserialize(TestConfigHelper.GetResourceString(TestConfigHelper.GetResourceName("en.loc.json")))!);
            Loc.Instance.AddLanguage(jsonLoader.Deserialize(TestConfigHelper.GetResourceString(TestConfigHelper.GetResourceName("zz.loc.json")))!);

            Loc.Instance.CurrentLanguageChanged += this.Instance_CurrentLanguageChanged;
            Loc.Instance.CurrentLanguageName = "English (US/CA)";
        }
        #endregion Constructor

        #region Fields
        private bool _isSettingSelectedNode = false;
        #endregion Fields

        #region Properties
        public ObservableCollection<LanguageVM> Languages { get; } = new();
        public LanguageVM? CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                var currentPath = _currentPath;
                _currentLanguage = value;
                NotifyPropertyChanged();
                CurrentPath = currentPath;
            }
        }
        private LanguageVM? _currentLanguage;
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                if (SetSelectedNode(_currentPath) is string path && path != _currentPath)
                {
                    _currentPath = path;
                }
                NotifyPropertyChanged();
            }
        }
        private string _currentPath = string.Empty;
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods
        private LanguageVM? GetLanguageVM(string languageName, StringComparison stringComparison = StringComparison.Ordinal)
            => Languages.FirstOrDefault(lang => lang.Name.Equals(languageName, stringComparison));
        public TreeNode? GetSelectedNode()
        {
            if (CurrentLanguage == null) return null;

            var path = CurrentPath.Split(Loc.PathSeparator);
            TreeNode? node = CurrentLanguage;
            for (int i = 0, i_max = path.Length; node != null && i < i_max; ++i)
            {
                var pathSegment = path[i];

                node = node.FindChild(n => n.Name.Equals(pathSegment, StringComparison.OrdinalIgnoreCase));
            }
            return node;
        }
        public void UpdateCurrentPath(TreeNode? newNode)
        {
            if (_isSettingSelectedNode) return;

            if (newNode != null)
            {
                _isSettingSelectedNode = true;
                CurrentPath = newNode.GetPath();
                _isSettingSelectedNode = false;
            }
            else
            {
                _isSettingSelectedNode = true;
                CurrentPath = string.Empty;
                _isSettingSelectedNode = false;
            }
        }
        private string[] SetSelectedNode(string[] path)
        {
            if (_isSettingSelectedNode || CurrentLanguage == null) return path;
            TreeNode? node = CurrentLanguage;
            for (int i = 0, i_max = path.Length; i < i_max && node != null; ++i)
            {
                var pathSegment = path[i];
                if (node.FindChild(n => n.Name.Equals(pathSegment, StringComparison.OrdinalIgnoreCase)) is TreeNode child)
                {
                    if (!child.Name.Equals(pathSegment, StringComparison.Ordinal))
                        path[i] = child.Name;
                    node.IsExpanded = true;
                    node = child;
                }
            }
            if (node == null) return path;
            _isSettingSelectedNode = true;
            node.IsSelected = true;
            _isSettingSelectedNode = false;
            return path;
        }
        private string SetSelectedNode(string path)
        {
            if (path == null) return null!;
            return string.Join(Loc.PathSeparator, SetSelectedNode(path.Split(Loc.PathSeparator)));
        }
        #endregion Methods

        #region EventHandlers

        #region Loc.Instance
        private void Instance_LanguageAdded(object? sender, LanguageEventArgs e)
        {
            Languages.Add(new(e.LanguageName, e.Translations));
        }
        private void Instance_LanguageRemoved(object? sender, LanguageEventArgs e)
        {
            if (GetLanguageVM(e.LanguageName) is LanguageVM vm)
            {
                Languages.Remove(vm);

                if (vm == CurrentLanguage)
                {
                    CurrentLanguage = null;
                }
            }
        }
        private void Instance_CurrentLanguageChanged(object? sender, CurrentLanguageChangedEventArgs e)
        {
            CurrentLanguage = GetLanguageVM(e.NewLanguageName);
        }
        #endregion Loc.Instance

        #endregion EventHandlers
    }
}

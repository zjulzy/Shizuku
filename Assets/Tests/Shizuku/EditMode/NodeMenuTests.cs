using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier1")]
    public sealed class NodeMenuTests
    {
        [Test]
        public void NodeMenuItem_DisplayName_UsesMenuPathLeaf()
        {
            var attribute = new NodeMenuItemAttribute("数学/Add (Float)");

            Assert.That(attribute.DisplayName, Is.EqualTo("Add (Float)"));
            Assert.That(NodeMenuItemAttribute.TryValidateMenuPath(attribute.MenuPath, out _), Is.True);
            Assert.That(NodeMenuItemAttribute.TryValidateMenuPath("Math/Add", out _), Is.False);
            Assert.That(NodeMenuItemAttribute.TryValidateMenuPath("数学/加法", out _), Is.False);
        }

        [Test]
        public void StaticNodeTitle_UsesMenuPathLeaf()
        {
            var node = new AddNode_Float();

            Assert.That(node.Title, Is.EqualTo("Add (Float)"));
        }

        [Test]
        public void AttributedNodeMenus_UseChineseGroupsAndEnglishNamesAndAreUnique()
        {
            var menuPaths = GetAttributedNodeTypes()
                .Select(type => type.GetCustomAttribute<NodeMenuItemAttribute>().MenuPath)
                .ToArray();

            Assert.That(menuPaths, Is.Not.Empty);
            Assert.That(menuPaths.Where(path =>
                !NodeMenuItemAttribute.TryValidateMenuPath(path, out _)), Is.Empty);

            var duplicatePaths = menuPaths
                .GroupBy(path => path, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicatePaths, Is.Empty);
        }

        [Test]
        public void SearchTree_UsesMenuPathAndHidesUnattributedInternalNodes()
        {
            var provider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
            try
            {
                var tree = provider.CreateSearchTree(new SearchWindowContext(Vector2.zero));

                Assert.That(tree.Any(entry => entry.level == 1 && entry.content.text == "数学"), Is.True);
                Assert.That(tree.Any(entry => entry.level == 2 && entry.content.text == "Add (Float)"), Is.True);
                Assert.That(tree.Any(entry => entry.content.text.Contains(nameof(BlueprintReturnNode))), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(provider);
            }
        }

        private static IEnumerable<Type> GetAttributedNodeTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    typeof(ShizukuNodeBase).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<NodeMenuItemAttribute>() != null);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}

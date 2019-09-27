using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Methods.Utilities
{
    static class TreeViewUtility
    {
        /// <summary>
        /// To get the full path of the TreeViewItem
        /// </summary>
        public static string GetFullPath(TreeViewItem node)
        {
            StringBuilder sb = new StringBuilder();
            while (node != null)
            {
                TreeViewModel.TreeViewModel model = node.DataContext as TreeViewModel.TreeViewModel;
                sb.Insert(0, "/" + model.Value);
                node = getParent(node);
            }

            return sb.ToString();
        }

        /// <summary>
        /// To get the parent of the item
        /// </summary>
        private static TreeViewItem getParent(TreeViewItem container)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(container);
            while (parent != null && !(parent is TreeViewItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as TreeViewItem;
        }

        public static void PopulateTreeView(dynamic nodeList, string path)
        {
            TreeViewModel.TreeViewModel node = null;
            string folder = string.Empty;
            int p = path.IndexOf('/');

            if (p == -1)
            {
                folder = path;
                path = string.Empty;
            }
            else
            {
                folder = path.Substring(0, p);
                path = path.Substring(p + 1, path.Length - (p + 1));
            }

            foreach (TreeViewModel.TreeViewModel item in nodeList.Items)
            {
                if (string.Equals(item.Value, folder)) { node = item; }
            }

            if (node == null)
            {
                node = new TreeViewModel.TreeViewModel(folder);
                nodeList.Items.Add(node);
            }

            if (path != "")
            {
                PopulateTreeView(node, path);
            }
        }
    }
}

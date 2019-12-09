using System.ComponentModel;
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

        public static void JumpToFolder(string node)
        {
            bool done = false;
            ICollectionView icv = FWindow.FMain.ViewModel.ItemsView;

            while (!done)
            {
                bool found = false;
                foreach (TreeViewModel.TreeViewModel tvi in icv)
                {
                    int sep = node.IndexOf("/");
                    if (node.StartsWith(tvi.Value) && node.Substring(0, sep > 0 ? sep : node.Length).Length == tvi.Value.Length)
                    {
                        found = true;
                        tvi.IsExpanded = true;
                        icv = tvi.ItemsView;
                        node = node.Substring(node.IndexOf("/") + 1);
                        if (node == tvi.Value && node.Length == tvi.Value.Length)
                        {
                            tvi.IsSelected = true;
                            done = true;
                        }
                        break;
                    }
                }

                done = !found && !done;
            }
        }
    }
}

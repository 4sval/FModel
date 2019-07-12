using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel.Forms
{
    //todo: refactor this shit + search with multiple word separated by spaces
    public partial class SearchFiles : Form
    {
        TypeAssistant _assistant;
        List<FileInfo> _myInfos = new List<FileInfo>();
        List<FileInfoFilter> _myFilteredInfos;
        private static string _fileName;
        private static Dictionary<string, string> _myInfosDict;
        private static Dictionary<string, string> _myFilteredInfosDict;
        public static string SfPath;
        public static bool IsClosed;
        public static bool FilesToSearch;
        public static string[] myItems;

        public SearchFiles()
        {
            InitializeComponent();

            _assistant = new TypeAssistant();
            _assistant.Idled += assistant_Idled;
        }

        private async void SearchFiles_Load(object sender, EventArgs e)
        {
            IsClosed = false;
            FilesToSearch = false;
            _myInfosDict = new Dictionary<string, string>();

            if (MainWindow.PakAsTxt != null)
            {
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                {
                    for (int i = 0; i < MainWindow.PakAsTxt.Length; i++)
                    {
                        if (MainWindow.PakAsTxt[i].Contains(".uasset") || MainWindow.PakAsTxt[i].Contains(".uexp") || MainWindow.PakAsTxt[i].Contains(".ubulk"))
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal))))
                            {
                                _myInfosDict.Add(MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal)), ThePak.CurrentUsedPak);

                                _fileName = MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal));
                                _myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = ThePak.CurrentUsedPak,
                                });
                            }
                        }
                        else
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.PakAsTxt[i]))
                            {
                                _myInfosDict.Add(MainWindow.PakAsTxt[i], ThePak.CurrentUsedPak);

                                _fileName = MainWindow.PakAsTxt[i];
                                _myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = ThePak.CurrentUsedPak,
                                });
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < MainWindow.PakAsTxt.Length; i++)
                    {
                        if (MainWindow.PakAsTxt[i].Contains(".uasset") || MainWindow.PakAsTxt[i].Contains(".uexp") || MainWindow.PakAsTxt[i].Contains(".ubulk"))
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal))))
                            {
                                _myInfosDict.Add(MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal)), ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(MainWindow.PakAsTxt[i])]);

                                _fileName = MainWindow.PakAsTxt[i].Substring(0, MainWindow.PakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal));
                                _myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(MainWindow.PakAsTxt[i])],
                                });
                            }
                        }
                        else
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.PakAsTxt[i]))
                            {
                                _myInfosDict.Add(MainWindow.PakAsTxt[i], ThePak.AllpaksDictionary[Path.GetFileName(MainWindow.PakAsTxt[i])]);

                                _fileName = MainWindow.PakAsTxt[i];
                                _myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = ThePak.AllpaksDictionary[Path.GetFileName(MainWindow.PakAsTxt[i])],
                                });
                            }
                        }
                    }
                }

                await Task.Run(() =>
                {
                    ShowItemsVirtual(_myInfos);
                });
            }
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (_myFilteredInfos == null || _myFilteredInfos.Count == 0)
            {
                var acc = _myInfos[e.ItemIndex];
                e.Item = new ListViewItem(
                    new[]
                    { acc.FileName, acc.PakFile });
            }
            else
            {
                var acc2 = _myFilteredInfos[e.ItemIndex];
                e.Item = new ListViewItem(
                    new[]
                    { acc2.FileName, acc2.PakFile });
            }
        }
        private void ShowItemsVirtual(List<FileInfo> infos)
        {
            Invoke(new Action(() =>
            {
                listView1.VirtualListSize = infos.Count;
            }));
        }
        private void ShowItemsVirtualFiltered(List<FileInfoFilter> infos)
        {
            Invoke(new Action(() =>
            {
                listView1.VirtualListSize = infos.Count;
            }));
        }

        private void FilterListView()
        {
            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new Action(FilterListView));
                return;
            }

            _myFilteredInfos = new List<FileInfoFilter>();
            _myFilteredInfosDict = new Dictionary<string, string>();
            listView1.BeginUpdate();
            listView1.VirtualListSize = 0;
            listView1.Invalidate();

            if (MainWindow.PakAsTxt != null)
            {
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text.Length > 2)
                    {
                        for (int i = 0; i < _myInfos.Count; i++)
                        {
                            string[] searchTextMultiple = textBox1.Text.Trim().Split(' ');
                            if (searchTextMultiple.Length > 1)
                            {
                                bool checkSearch = false;
                                for (int s = 0; s < searchTextMultiple.Length; ++s)
                                {
                                    checkSearch = Utilities.CaseInsensitiveContains(_myInfos[i].FileName, searchTextMultiple[s]);
                                    if (!checkSearch)
                                        break;
                                }

                                if (checkSearch)
                                {
                                    if (_myInfos[i].FileName.Contains(".uasset") || _myInfos[i].FileName.Contains(".uexp") || _myInfos[i].FileName.Contains(".ubulk"))
                                    {
                                        if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                        {
                                            _myFilteredInfosDict.Add(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), ThePak.CurrentUsedPak);

                                            _fileName = _myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                            _myFilteredInfos.Add(new FileInfoFilter
                                            {
                                                FileName = _fileName,
                                                PakFile = ThePak.CurrentUsedPak,
                                            });
                                        }
                                    }
                                    else
                                    {
                                        if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName))
                                        {
                                            _myFilteredInfosDict.Add(_myInfos[i].FileName, ThePak.CurrentUsedPak);

                                            _fileName = _myInfos[i].FileName;
                                            _myFilteredInfos.Add(new FileInfoFilter
                                            {
                                                FileName = _fileName,
                                                PakFile = ThePak.CurrentUsedPak,
                                            });
                                        }
                                    }

                                    ShowItemsVirtualFiltered(_myFilteredInfos);
                                }
                            }
                            else if (Utilities.CaseInsensitiveContains(_myInfos[i].FileName, textBox1.Text))
                            {
                                if (_myInfos[i].FileName.Contains(".uasset") || _myInfos[i].FileName.Contains(".uexp") || _myInfos[i].FileName.Contains(".ubulk"))
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                    {
                                        _myFilteredInfosDict.Add(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), ThePak.CurrentUsedPak);

                                        _fileName = _myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = ThePak.CurrentUsedPak,
                                        });
                                    }
                                }
                                else
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName))
                                    {
                                        _myFilteredInfosDict.Add(_myInfos[i].FileName, ThePak.CurrentUsedPak);

                                        _fileName = _myInfos[i].FileName;
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = ThePak.CurrentUsedPak,
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(_myFilteredInfos);
                            }
                        }
                    }
                    else
                    {
                        ShowItemsVirtual(_myInfos);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text.Length > 2)
                    {
                        for (int i = 0; i < _myInfos.Count; i++)
                        {
                            string[] searchTextMultiple = textBox1.Text.Trim().Split(' ');
                            if (searchTextMultiple.Length > 1)
                            {
                                bool checkSearch = false;
                                for (int s = 0; s < searchTextMultiple.Length; ++s)
                                {
                                    checkSearch = Utilities.CaseInsensitiveContains(_myInfos[i].FileName, searchTextMultiple[s]);
                                    if (!checkSearch)
                                        break;
                                }

                                if (checkSearch)
                                {
                                    if (_myInfos[i].FileName.Contains(".uasset") || _myInfos[i].FileName.Contains(".uexp") || _myInfos[i].FileName.Contains(".ubulk"))
                                    {
                                        if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                        {
                                            _myFilteredInfosDict.Add(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(_myInfos[i].FileName)]);

                                            _fileName = _myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                            _myFilteredInfos.Add(new FileInfoFilter
                                            {
                                                FileName = _fileName,
                                                PakFile = ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(_myInfos[i].FileName)],
                                            });
                                        }
                                    }
                                    else
                                    {
                                        if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName))
                                        {
                                            _myFilteredInfosDict.Add(_myInfos[i].FileName, ThePak.AllpaksDictionary[Path.GetFileName(_myInfos[i].FileName)]);

                                            _fileName = _myInfos[i].FileName;
                                            _myFilteredInfos.Add(new FileInfoFilter
                                            {
                                                FileName = _fileName,
                                                PakFile = ThePak.AllpaksDictionary[Path.GetFileName(_myInfos[i].FileName)],
                                            });
                                        }
                                    }

                                    ShowItemsVirtualFiltered(_myFilteredInfos);
                                }
                            }
                            else if (Utilities.CaseInsensitiveContains(_myInfos[i].FileName, textBox1.Text))
                            {
                                if (_myInfos[i].FileName.Contains(".uasset") || _myInfos[i].FileName.Contains(".uexp") || _myInfos[i].FileName.Contains(".ubulk"))
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                    {
                                        _myFilteredInfosDict.Add(_myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(_myInfos[i].FileName)]);

                                        _fileName = _myInfos[i].FileName.Substring(0, _myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = ThePak.AllpaksDictionary[Path.GetFileNameWithoutExtension(_myInfos[i].FileName)],
                                        });
                                    }
                                }
                                else
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(_myInfos[i].FileName))
                                    {
                                        _myFilteredInfosDict.Add(_myInfos[i].FileName, ThePak.AllpaksDictionary[Path.GetFileName(_myInfos[i].FileName)]);

                                        _fileName = _myInfos[i].FileName;
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = ThePak.AllpaksDictionary[Path.GetFileName(_myInfos[i].FileName)],
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(_myFilteredInfos);
                            }
                        }
                    }
                    else
                    {
                        ShowItemsVirtual(_myInfos);
                    }
                }
            }

            listView1.EndUpdate();
        }
        void assistant_Idled(object sender, EventArgs e)
        {
            Invoke(
            new MethodInvoker(() =>
            {
                FilterListView();
            }));
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _assistant.TextChanged();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection col = listView1.SelectedIndices;
            SfPath = listView1.Items[col[0]].Text;

            IsClosed = true;
            Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (true)
            {
                button1.Enabled = true;
                button2.Enabled = true;
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection col = listView1.SelectedIndices;
            myItems = new string[col.Count];
            for (int i = 0; i < col.Count; i++) //ADD SELECTED ITEM TO ARRAY
            {
                myItems[i] = listView1.Items[col[i]].Text.Substring(listView1.Items[col[i]].Text.LastIndexOf("/") + 1);
            }

            FilesToSearch = true;
            Close();
        }
    }

    class FileInfo
    {
        public string FileName
        {
            get;
            set;
        }
        public string PakFile
        {
            get;
            set;
        }
    }
    class FileInfoFilter
    {
        public string FileName
        {
            get;
            set;
        }
        public string PakFile
        {
            get;
            set;
        }
    }
}

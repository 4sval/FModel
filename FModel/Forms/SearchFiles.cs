using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel.Forms
{
    public partial class SearchFiles : Form
    {
        TypeAssistant assistant;
        List<FileInfo> myInfos = new List<FileInfo>();
        List<FileInfoFilter> _myFilteredInfos;
        private static string _fileName;
        private static Dictionary<string, string> _myInfosDict;
        private static Dictionary<string, string> _myFilteredInfosDict;
        public static string SfPath;
        public static bool IsClosed;

        public SearchFiles()
        {
            InitializeComponent();

            assistant = new TypeAssistant();
            assistant.Idled += assistant_Idled;
        }

        private async void SearchFiles_Load(object sender, EventArgs e)
        {
            IsClosed = false;
            _myInfosDict = new Dictionary<string, string>();

            if (MainWindow.pakAsTxt != null)
            {
                if (MainWindow.CurrentUsedPakGuid != null && MainWindow.CurrentUsedPakGuid != "0-0-0-0")
                {
                    for (int i = 0; i < MainWindow.pakAsTxt.Length; i++)
                    {
                        if (MainWindow.pakAsTxt[i].Contains(".uasset") || MainWindow.pakAsTxt[i].Contains(".uexp") || MainWindow.pakAsTxt[i].Contains(".ubulk"))
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal))))
                            {
                                _myInfosDict.Add(MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal)), MainWindow.CurrentUsedPak);

                                _fileName = MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal));
                                myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = MainWindow.CurrentUsedPak,
                                });
                            }
                        }
                        else
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.pakAsTxt[i]))
                            {
                                _myInfosDict.Add(MainWindow.pakAsTxt[i], MainWindow.CurrentUsedPak);

                                _fileName = MainWindow.pakAsTxt[i];
                                myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = MainWindow.CurrentUsedPak,
                                });
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < MainWindow.pakAsTxt.Length; i++)
                    {
                        if (MainWindow.pakAsTxt[i].Contains(".uasset") || MainWindow.pakAsTxt[i].Contains(".uexp") || MainWindow.pakAsTxt[i].Contains(".ubulk"))
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal))))
                            {
                                _myInfosDict.Add(MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal)), MainWindow.AllpaksDictionary[Path.GetFileNameWithoutExtension(MainWindow.pakAsTxt[i])]);

                                _fileName = MainWindow.pakAsTxt[i].Substring(0, MainWindow.pakAsTxt[i].LastIndexOf(".", StringComparison.Ordinal));
                                myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = MainWindow.AllpaksDictionary[Path.GetFileNameWithoutExtension(MainWindow.pakAsTxt[i])],
                                });
                            }
                        }
                        else
                        {
                            if (!_myInfosDict.ContainsKey(MainWindow.pakAsTxt[i]))
                            {
                                _myInfosDict.Add(MainWindow.pakAsTxt[i], MainWindow.AllpaksDictionary[Path.GetFileName(MainWindow.pakAsTxt[i])]);

                                _fileName = MainWindow.pakAsTxt[i];
                                myInfos.Add(new FileInfo
                                {
                                    FileName = _fileName,
                                    PakFile = MainWindow.AllpaksDictionary[Path.GetFileName(MainWindow.pakAsTxt[i])],
                                });
                            }
                        }
                    }
                }

                await Task.Run(() =>
                {
                    ShowItemsVirtual(myInfos);
                });
            }
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (_myFilteredInfos == null || _myFilteredInfos.Count == 0)
            {
                var acc = myInfos[e.ItemIndex];
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

            if (MainWindow.pakAsTxt != null)
            {
                if (MainWindow.CurrentUsedPakGuid != null && MainWindow.CurrentUsedPakGuid != "0-0-0-0")
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text.Length > 2)
                    {
                        for (int i = 0; i < myInfos.Count; i++)
                        {
                            if (MainWindow.CaseInsensitiveContains(myInfos[i].FileName, textBox1.Text))
                            {
                                if (myInfos[i].FileName.Contains(".uasset") || myInfos[i].FileName.Contains(".uexp") || myInfos[i].FileName.Contains(".ubulk"))
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                    {
                                        _myFilteredInfosDict.Add(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), MainWindow.CurrentUsedPak);

                                        _fileName = myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = MainWindow.CurrentUsedPak,
                                        });
                                    }
                                }
                                else
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(myInfos[i].FileName))
                                    {
                                        _myFilteredInfosDict.Add(myInfos[i].FileName, MainWindow.CurrentUsedPak);

                                        _fileName = myInfos[i].FileName;
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = MainWindow.CurrentUsedPak,
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(_myFilteredInfos);
                            }
                        }
                    }
                    else
                    {
                        ShowItemsVirtual(myInfos);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text.Length > 2)
                    {
                        for (int i = 0; i < myInfos.Count; i++)
                        {
                            if (MainWindow.CaseInsensitiveContains(myInfos[i].FileName, textBox1.Text))
                            {
                                if (myInfos[i].FileName.Contains(".uasset") || myInfos[i].FileName.Contains(".uexp") || myInfos[i].FileName.Contains(".ubulk"))
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal))))
                                    {
                                        _myFilteredInfosDict.Add(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal)), MainWindow.AllpaksDictionary[Path.GetFileNameWithoutExtension(myInfos[i].FileName)]);

                                        _fileName = myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".", StringComparison.Ordinal));
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = MainWindow.AllpaksDictionary[Path.GetFileNameWithoutExtension(myInfos[i].FileName)],
                                        });
                                    }
                                }
                                else
                                {
                                    if (!_myFilteredInfosDict.ContainsKey(myInfos[i].FileName))
                                    {
                                        _myFilteredInfosDict.Add(myInfos[i].FileName, MainWindow.AllpaksDictionary[Path.GetFileName(myInfos[i].FileName)]);

                                        _fileName = myInfos[i].FileName;
                                        _myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = _fileName,
                                            PakFile = MainWindow.AllpaksDictionary[Path.GetFileName(myInfos[i].FileName)],
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(_myFilteredInfos);
                            }
                        }
                    }
                    else
                    {
                        ShowItemsVirtual(myInfos);
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
            assistant.TextChanged();
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
            }
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

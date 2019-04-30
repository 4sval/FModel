using FModel.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel.Forms
{
    public partial class SearchFiles : Form
    {
        TypeAssistant assistant;
        List<FileInfo> myInfos = new List<FileInfo>();
        List<FileInfoFilter> myFilteredInfos;
        private static string fileName;
        private static Dictionary<string, string> myInfosDict;
        private static Dictionary<string, string> myFilteredInfosDict;
        public static string sfPath;
        public static bool isClosed;

        public SearchFiles()
        {
            InitializeComponent();

            assistant = new TypeAssistant();
            assistant.Idled += assistant_Idled;
        }

        private async void SearchFiles_Load(object sender, EventArgs e)
        {
            isClosed = false;
            myInfosDict = new Dictionary<string, string>();

            if (MainWindow.PAKasTXT != null)
            {
                if (MainWindow.currentUsedPAKGUID != null && MainWindow.currentUsedPAKGUID != "0-0-0-0")
                {
                    for (int i = 0; i < MainWindow.PAKasTXT.Length; i++)
                    {
                        if (MainWindow.PAKasTXT[i].Contains(".uasset") || MainWindow.PAKasTXT[i].Contains(".uexp") || MainWindow.PAKasTXT[i].Contains(".ubulk"))
                        {
                            if (!myInfosDict.ContainsKey(MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf("."))))
                            {
                                myInfosDict.Add(MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf(".")), MainWindow.currentUsedPAK);

                                fileName = MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf("."));
                                myInfos.Add(new FileInfo
                                {
                                    FileName = fileName,
                                    PAKFile = MainWindow.currentUsedPAK,
                                });
                            }
                        }
                        else
                        {
                            if (!myInfosDict.ContainsKey(MainWindow.PAKasTXT[i]))
                            {
                                myInfosDict.Add(MainWindow.PAKasTXT[i], MainWindow.currentUsedPAK);

                                fileName = MainWindow.PAKasTXT[i];
                                myInfos.Add(new FileInfo
                                {
                                    FileName = fileName,
                                    PAKFile = MainWindow.currentUsedPAK,
                                });
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < MainWindow.PAKasTXT.Length; i++)
                    {
                        if (MainWindow.PAKasTXT[i].Contains(".uasset") || MainWindow.PAKasTXT[i].Contains(".uexp") || MainWindow.PAKasTXT[i].Contains(".ubulk"))
                        {
                            if (!myInfosDict.ContainsKey(MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf("."))))
                            {
                                myInfosDict.Add(MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf(".")), MainWindow.AllPAKsDictionary[Path.GetFileNameWithoutExtension(MainWindow.PAKasTXT[i])]);

                                fileName = MainWindow.PAKasTXT[i].Substring(0, MainWindow.PAKasTXT[i].LastIndexOf("."));
                                myInfos.Add(new FileInfo
                                {
                                    FileName = fileName,
                                    PAKFile = MainWindow.AllPAKsDictionary[Path.GetFileNameWithoutExtension(MainWindow.PAKasTXT[i])],
                                });
                            }
                        }
                        else
                        {
                            if (!myInfosDict.ContainsKey(MainWindow.PAKasTXT[i]))
                            {
                                myInfosDict.Add(MainWindow.PAKasTXT[i], MainWindow.AllPAKsDictionary[Path.GetFileName(MainWindow.PAKasTXT[i])]);

                                fileName = MainWindow.PAKasTXT[i];
                                myInfos.Add(new FileInfo
                                {
                                    FileName = fileName,
                                    PAKFile = MainWindow.AllPAKsDictionary[Path.GetFileName(MainWindow.PAKasTXT[i])],
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
            if (myFilteredInfos == null || myFilteredInfos.Count == 0)
            {
                var acc = myInfos[e.ItemIndex];
                e.Item = new ListViewItem(
                    new string[]
                    { acc.FileName, acc.PAKFile });
            }
            else
            {
                var acc2 = myFilteredInfos[e.ItemIndex];
                e.Item = new ListViewItem(
                    new string[]
                    { acc2.FileName, acc2.PAKFile });
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

        private void filterListView()
        {
            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new Action(filterListView));
                return;
            }

            myFilteredInfos = new List<FileInfoFilter>();
            myFilteredInfosDict = new Dictionary<string, string>();
            listView1.BeginUpdate();
            listView1.VirtualListSize = 0;
            listView1.Invalidate();

            if (MainWindow.PAKasTXT != null)
            {
                if (MainWindow.currentUsedPAKGUID != null && MainWindow.currentUsedPAKGUID != "0-0-0-0")
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text.Length > 2)
                    {
                        for (int i = 0; i < myInfos.Count; i++)
                        {
                            if (MainWindow.CaseInsensitiveContains(myInfos[i].FileName, textBox1.Text))
                            {
                                if (myInfos[i].FileName.Contains(".uasset") || myInfos[i].FileName.Contains(".uexp") || myInfos[i].FileName.Contains(".ubulk"))
                                {
                                    if (!myFilteredInfosDict.ContainsKey(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf("."))))
                                    {
                                        myFilteredInfosDict.Add(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".")), MainWindow.currentUsedPAK);

                                        fileName = myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf("."));
                                        myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = fileName,
                                            PAKFile = MainWindow.currentUsedPAK,
                                        });
                                    }
                                }
                                else
                                {
                                    if (!myFilteredInfosDict.ContainsKey(myInfos[i].FileName))
                                    {
                                        myFilteredInfosDict.Add(myInfos[i].FileName, MainWindow.currentUsedPAK);

                                        fileName = myInfos[i].FileName;
                                        myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = fileName,
                                            PAKFile = MainWindow.currentUsedPAK,
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(myFilteredInfos);
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
                                    if (!myFilteredInfosDict.ContainsKey(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf("."))))
                                    {
                                        myFilteredInfosDict.Add(myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf(".")), MainWindow.AllPAKsDictionary[Path.GetFileNameWithoutExtension(myInfos[i].FileName)]);

                                        fileName = myInfos[i].FileName.Substring(0, myInfos[i].FileName.LastIndexOf("."));
                                        myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = fileName,
                                            PAKFile = MainWindow.AllPAKsDictionary[Path.GetFileNameWithoutExtension(myInfos[i].FileName)],
                                        });
                                    }
                                }
                                else
                                {
                                    if (!myFilteredInfosDict.ContainsKey(myInfos[i].FileName))
                                    {
                                        myFilteredInfosDict.Add(myInfos[i].FileName, MainWindow.AllPAKsDictionary[Path.GetFileName(myInfos[i].FileName)]);

                                        fileName = myInfos[i].FileName;
                                        myFilteredInfos.Add(new FileInfoFilter
                                        {
                                            FileName = fileName,
                                            PAKFile = MainWindow.AllPAKsDictionary[Path.GetFileName(myInfos[i].FileName)],
                                        });
                                    }
                                }

                                ShowItemsVirtualFiltered(myFilteredInfos);
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
            this.Invoke(
            new MethodInvoker(() =>
            {
                filterListView();
            }));
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            assistant.TextChanged();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection col = listView1.SelectedIndices;
            sfPath = listView1.Items[col[0]].Text;

            isClosed = true;
            Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices != null)
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
        public string PAKFile
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
        public string PAKFile
        {
            get;
            set;
        }
    }
}

using System;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.GUICommon;
using ChaKi.Service.TagSetEdit;
using System.Collections.Generic;
using AnnotationTag = ChaKi.Entity.Corpora.Annotations.Tag;

namespace ChaKi.TagSetDefinitionEditor
{
    public partial class TagSetDialog : Form
    {
        private ViewModel m_ViewModel;
        private TagSetEditService m_Service;
        private bool lockUpdate = false;

        private const string CREATE_VERSION_STRING = "Create New...";

        public TagSetDialog(string dbfile, string tagsetname)
        {
            InitializeComponent();

            // Setup Corpus object
            Corpus c = Corpus.CreateFromFile(dbfile);

            // Begin Service
            m_Service = new TagSetEditService(c, tagsetname);

            // Create ViewModel
            m_ViewModel = new ViewModel()
            {
                Corpus = c,
                TagSet = m_Service.TagSet,
                CurrentVersion = m_Service.CurrentVersion
            };

            StateTransition(ViewModel.Triggers.Initialize);

            // Update View
            UpdateContents();
        }

        private void UpdateContents()
        {
            lockUpdate = true;
            try
            {
                m_ViewModel.VersionList = m_Service.GetVersionList();
                m_ViewModel.VersionList.Add(new KeyValuePair<string, bool>(CREATE_VERSION_STRING, false));
                m_ViewModel.CurrentVersion = m_Service.CurrentVersion;

                this.textBox2.Text = m_ViewModel.Corpus.Name;
                this.textBox1.Text = m_ViewModel.TagSet.Name;
                this.comboBox1.DataSource = m_ViewModel.VersionListAsString;
                this.comboBox1.SelectedIndex = m_ViewModel.IndexOfCurrentVersion;

                m_ViewModel.Segments = m_Service.QueryTags(AnnotationTag.SEGMENT);
                m_ViewModel.Links = m_Service.QueryTags(AnnotationTag.LINK);
                m_ViewModel.Groups = m_Service.QueryTags(AnnotationTag.GROUP);

                this.tagDefGrid1.DataSource = m_ViewModel.Segments;
                this.tagDefGrid2.DataSource = m_ViewModel.Links;
                this.tagDefGrid3.DataSource = m_ViewModel.Groups;
            }
            finally
            {
                lockUpdate = false;
            }
        }

        private void StateTransition(ViewModel.Triggers trigger)
        {
            switch (m_ViewModel.State)
            {
                case ViewModel.States.Browsing:
                    if (trigger == ViewModel.Triggers.BeginEdit)
                    {
                        this.tagDefGrid1.IsEditMode = true;
                        this.tagDefGrid2.IsEditMode = true;
                        this.tagDefGrid3.IsEditMode = true;
                        this.button1.Enabled = false; // "Delete Version" Button
                        this.button2.Enabled = true; // "Save" Button
                        m_ViewModel.State = ViewModel.States.Editing;
                    }
                    else if (trigger == ViewModel.Triggers.Initialize)
                    {
                        this.tagDefGrid1.IsEditMode = false;
                        this.tagDefGrid2.IsEditMode = false;
                        this.tagDefGrid3.IsEditMode = false;
                        this.button1.Enabled = true; // "Delete Version" Button
                        this.button2.Enabled = false; // "Save" Button
                        m_ViewModel.State = ViewModel.States.Browsing;
                    }
                    break;
                case ViewModel.States.Editing:
                    if (trigger == ViewModel.Triggers.EndEdit)
                    {
                        this.tagDefGrid1.IsEditMode = false;
                        this.tagDefGrid2.IsEditMode = false;
                        this.tagDefGrid3.IsEditMode = false;
                        this.button1.Enabled = true; // "Delete Version" Button
                        this.button2.Enabled = false; // "Save" Button
                        m_ViewModel.State = ViewModel.States.Browsing;
                    }
                    break;
            }
            if (m_ViewModel.TagSet.Versions.Count <= 1)
            {
                // Version Listが1個以下なら"Delete Version"できないようにする.
                this.button1.Enabled = false;
            }
        }

        private bool CreateNewVersion()
        {
            EnterNewVersion dlg = new EnterNewVersion();
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            try
            {
                m_Service.BeginTransaction();
                m_Service.CreateNewVersion(dlg.VersionName);
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
                return false;
            }

            StateTransition(ViewModel.Triggers.BeginEdit);
            UpdateContents();

            return true;
        }

        // "Delete Version" Command
        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Deleting Current Version", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                return;
            }

            try
            {
                if (!m_Service.DeleteCurrentVersion())
                {
                    MessageBox.Show("Cannot delete the version. (Contains active Tags)");
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
            }
            UpdateContents();
        }

        // Save Command
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // Update ViewModel
                m_Service.UpdateTags(m_ViewModel.Segments, m_ViewModel.Links, m_ViewModel.Groups);

                // Commit
                m_Service.Commit();
                StateTransition(ViewModel.Triggers.EndEdit);
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
            }
            UpdateContents();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lockUpdate) return;
            lockUpdate = true;
            try
            {
                int index = this.comboBox1.SelectedIndex;
                string ver = (string)this.comboBox1.SelectedItem;
                if (ver == CREATE_VERSION_STRING)
                {
                    if (m_ViewModel.State == ViewModel.States.Editing)
                    {
                        if (MessageBox.Show("End Editing?", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            m_Service.Rollback();
                            StateTransition(ViewModel.Triggers.EndEdit);
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (!CreateNewVersion())
                    {
                        // ComboBoxの選択を元に戻す
                        this.comboBox1.SelectedIndex = m_ViewModel.IndexOfCurrentVersion;
                    }
                }
                else
                {
                    if (index != m_ViewModel.IndexOfCurrentVersion)
                    {
                        if (m_ViewModel.State == ViewModel.States.Editing)
                        {
                            if (MessageBox.Show("End Editing?", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
                            {
                                m_Service.Rollback();
                                StateTransition(ViewModel.Triggers.EndEdit);
                            }
                            else
                            {
                                return;
                            }
                        }
                        // CurrentVersionを変更
                        m_ViewModel.CurrentVersion = m_Service.SetVersion(ver);
                        UpdateContents();
                    }
                }
            }
            finally
            {
                lockUpdate = false;
            }
        }

        // TagSet Name 変更
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Update ViewModel
                m_Service.ChangeTagSetName(this.textBox1.Text);
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
            }
        }
    }
}

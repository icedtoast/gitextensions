﻿using System;
using System.Windows.Forms;
using GitCommands;
using System.Drawing;
using ResourceManager.Translation;

namespace GitUI
{
    public partial class FormApplyPatch : GitExtensionsForm
    {
        #region Translation

        private readonly TranslationString _conflictResolvedText =
            new TranslationString("Conflicts resolved");
        private readonly TranslationString _conflictMergetoolText =
            new TranslationString("Solve conflicts");

        private readonly TranslationString _selectPatchFileFilter =
            new TranslationString("Patch file (*.Patch)");
        private readonly TranslationString _selectPatchFileCaption =
            new TranslationString("Select patch file");

        private readonly TranslationString _noFileSelectedText =
            new TranslationString("Please select a patch to apply");

        private readonly TranslationString _applyPatchMsgBox =
            new TranslationString("Apply patch");

        #endregion

        public FormApplyPatch()
            : base(true)
        {
            InitializeComponent(); Translate();
            EnableButtons();
        }

        public void SetPatchFile(string name)
        {
            PatchFile.Text = name;
        }

        private void EnableButtons()
        {
            if (GitModule.Current.InTheMiddleOfPatch())
            {
                Apply.Enabled = false;
                IgnoreWhitespace.Enabled = false;
                PatchFileMode.Enabled = false;
                PatchDirMode.Enabled = false;
                AddFiles.Enabled = true;
                Resolved.Enabled = !GitModule.Current.InTheMiddleOfConflictedMerge();
                Mergetool.Enabled = GitModule.Current.InTheMiddleOfConflictedMerge();
                Skip.Enabled = true;
                Abort.Enabled = true;

                PatchFile.Enabled = false;
                PatchFile.ReadOnly = false;
                BrowsePatch.Enabled = false;
                PatchDir.Enabled = false;
                PatchDir.ReadOnly = false;
                BrowseDir.Enabled = false;
            }
            else
            {
                PatchFile.Enabled = PatchFileMode.Checked;
                PatchFile.ReadOnly = !PatchFileMode.Checked;
                BrowsePatch.Enabled = PatchFileMode.Checked;
                PatchDir.Enabled = PatchDirMode.Checked;
                PatchDir.ReadOnly = !PatchDirMode.Checked;
                BrowseDir.Enabled = PatchDirMode.Checked;

                Apply.Enabled = true;
                IgnoreWhitespace.Enabled = true;
                PatchFileMode.Enabled = true;
                PatchDirMode.Enabled = true;
                AddFiles.Enabled = false;
                Resolved.Enabled = false;
                Mergetool.Enabled = false;
                Skip.Enabled = false;
                Abort.Enabled = false;
            }

            patchGrid1.Initialize();

            SolveMergeconflicts.Visible = GitModule.Current.InTheMiddleOfConflictedMerge();

            Resolved.Text = _conflictResolvedText.Text;
            Mergetool.Text = _conflictMergetoolText.Text;
            ContinuePanel.BackColor = Color.Transparent;
            MergeToolPanel.BackColor = Color.Transparent;

            if (GitModule.Current.InTheMiddleOfConflictedMerge())
            {
                Mergetool.Text = ">" + _conflictMergetoolText.Text + "<";
                Mergetool.Focus();
                AcceptButton = Mergetool;
                MergeToolPanel.BackColor = Color.Black;
            }
            else if (GitModule.Current.InTheMiddleOfPatch())
            {
                Resolved.Text = ">" + _conflictResolvedText.Text + "<";
                Resolved.Focus();
                AcceptButton = Resolved;
                ContinuePanel.BackColor = Color.Black;
            }
        }

        private string SelectPatchFile(string initialDirectory)
        {
            using (var dialog = new OpenFileDialog
                             {
                                 Filter = _selectPatchFileFilter.Text + "|*.Patch",
                                 InitialDirectory = initialDirectory,
                                 Title = _selectPatchFileCaption.Text
                             })
            {
                return (dialog.ShowDialog(this) == DialogResult.OK) ? dialog.FileName : PatchFile.Text;
            }
        }

        private void BrowsePatch_Click(object sender, EventArgs e)
        {
            PatchFile.Text = SelectPatchFile(@".");
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(PatchFile.Text) && string.IsNullOrEmpty(PatchDir.Text))
            {
                MessageBox.Show(this, _noFileSelectedText.Text);
                return;
            }
            Cursor.Current = Cursors.WaitCursor;
            if (PatchFileMode.Checked)
                if (IgnoreWhitespace.Checked)
                {
                    FormProcess.ShowDialog(this, GitCommandHelpers.PatchCmdIgnoreWhitespace(PatchFile.Text));
                }
                else
                {
                    FormProcess.ShowDialog(this, GitCommandHelpers.PatchCmd(PatchFile.Text));
                }
            else
                if (IgnoreWhitespace.Checked)
                {
                    GitCommandHelpers.ApplyPatch(PatchDir.Text, GitCommandHelpers.PatchDirCmdIgnoreWhitespace());
                }
                else
                {
                    GitCommandHelpers.ApplyPatch(PatchDir.Text, GitCommandHelpers.PatchDirCmd());
                }

            EnableButtons();

            if (!GitModule.Current.InTheMiddleOfConflictedMerge() && !GitModule.Current.InTheMiddleOfRebase() && !GitModule.Current.InTheMiddleOfPatch())
                Close();
            Cursor.Current = Cursors.Default;
        }

        private void Mergetool_Click(object sender, EventArgs e)
        {
            GitUICommands.Instance.StartResolveConflictsDialog(this);
            EnableButtons();
        }

        private void Skip_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            FormProcess.ShowDialog(this, GitCommandHelpers.SkipCmd());
            EnableButtons();
            Cursor.Current = Cursors.Default;
        }

        private void Resolved_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            FormProcess.ShowDialog(this, (GitCommandHelpers.ResolvedCmd()));
            EnableButtons();
            Cursor.Current = Cursors.Default;
        }

        private void Abort_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            FormProcess.ShowDialog(this, GitCommandHelpers.AbortCmd());
            EnableButtons();
            Cursor.Current = Cursors.Default;
        }

        private void AddFiles_Click(object sender, EventArgs e)
        {
            GitUICommands.Instance.StartAddFilesDialog(this);
        }

        private void MergePatch_Load(object sender, EventArgs e)
        {
            PatchFile.Select();
            
            Text = _applyPatchMsgBox.Text + " (" + GitModule.CurrentWorkingDir + ")";
            IgnoreWhitespace.Checked = Settings.ApplyPatchIgnoreWhitespace;
        }

        private void BrowseDir_Click(object sender, EventArgs e)
        {
            using (var browseDialog = new FolderBrowserDialog())
            {

                if (browseDialog.ShowDialog(this) == DialogResult.OK)
                {
                    PatchDir.Text = browseDialog.SelectedPath;
                }
            }
        }

        private void PatchFileMode_CheckedChanged(object sender, EventArgs e)
        {
            EnableButtons();
        }

        private void PatchDirMode_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void SolveMergeconflicts_Click(object sender, EventArgs e)
        {
            Mergetool_Click(sender, e);
        }

        private void IgnoreWhitespace_CheckedChanged(object sender, EventArgs e)
        {
            Settings.ApplyPatchIgnoreWhitespace = IgnoreWhitespace.Checked;
        }
    }
}

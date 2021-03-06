﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitCommands;
using System.Windows.Forms;
using GitUI.Editor;
using ICSharpCode.TextEditor.Util;
using System.Diagnostics;
using System.Drawing;

namespace GitUI
{
    public static class GitUIExtensions
    {

        public enum DiffWithRevisionKind
        {
            DiffBaseLocal,
            DiffRemoteLocal,
            DiffAsSelected
        }

        public static void OpenWithDifftool(this RevisionGrid grid, string fileName, DiffWithRevisionKind diffKind)
        {
            IList<GitRevision> revisions = grid.GetSelectedRevisions();

            if (revisions.Count == 0)
                return;

            string output;
            if (diffKind == DiffWithRevisionKind.DiffBaseLocal)
            {
                if (revisions[0].ParentGuids.Length == 0)
                    return;
                output = GitModule.Current.OpenWithDifftool(fileName, revisions[0].ParentGuids[0]);

            }
            else if (diffKind == DiffWithRevisionKind.DiffRemoteLocal)
                output = GitModule.Current.OpenWithDifftool(fileName, revisions[0].Guid);
            else
            {
                string firstRevision = revisions[0].Guid;
                var secondRevision = revisions.Count == 2 ? revisions[1].Guid : null;

                //to simplify if-ology
                if (GitRevision.IsArtificial(secondRevision) && firstRevision != GitRevision.UncommittedWorkingDirGuid)
                {
                    firstRevision = secondRevision;
                    secondRevision = revisions[0].Guid;
                }

                string extraDiffArgs = null;

                if (firstRevision == GitRevision.UncommittedWorkingDirGuid) //working dir changes
                {
                    if (secondRevision == null || secondRevision == GitRevision.IndexGuid)
                    {
                        firstRevision = string.Empty;
                        secondRevision = string.Empty;
                    }
                    else
                    {
                        // rev2 vs working dir changes
                        firstRevision = secondRevision;
                        secondRevision = string.Empty;
                    }
                }
                if (firstRevision == GitRevision.IndexGuid) //index
                {
                    if (secondRevision == null)
                    {
                        firstRevision = string.Empty;
                        secondRevision = string.Empty;
                        extraDiffArgs = string.Join(" ", extraDiffArgs, "--cached");
                    }
                    else //rev1 vs index
                    {
                        firstRevision = secondRevision;
                        secondRevision = string.Empty;
                        extraDiffArgs = string.Join(" ", extraDiffArgs, "--cached");
                    }
                }

                Debug.Assert(!GitRevision.IsArtificial(firstRevision), string.Join(" ", firstRevision, secondRevision));

                if (secondRevision == null)
                    secondRevision = firstRevision + "^";

                output = GitModule.Current.OpenWithDifftool(fileName, firstRevision, secondRevision, extraDiffArgs);
            }

            if (!string.IsNullOrEmpty(output))
                MessageBox.Show(grid, output);
        }

        public static string GetSelectedPatch(this FileViewer diffViewer, RevisionGrid grid, GitItemStatus file)
        {
            IList<GitRevision> revisions = grid.GetSelectedRevisions();

            if (revisions.Count == 0)
                return null;

            string firstRevision = revisions[0].Guid;
            var secondRevision = revisions.Count == 2 ? revisions[1].Guid : null;

            //to simplify if-ology
            if (GitRevision.IsArtificial(secondRevision) && firstRevision != GitRevision.UncommittedWorkingDirGuid)
            {
                firstRevision = secondRevision;
                secondRevision = revisions[0].Guid;
            }

            string extraDiffArgs = null;

            if (firstRevision == GitRevision.UncommittedWorkingDirGuid) //working dir changes
            {
                if (secondRevision == null || secondRevision == GitRevision.IndexGuid)
                {
                    if (file.IsTracked)
                    {
                        return ProcessDiffText(GitModule.Current.GetCurrentChanges(file.Name, file.OldName, false,
                            diffViewer.GetExtraDiffArguments(), diffViewer.Encoding), file.IsSubmodule);
                    }

                    return FileReader.ReadFileContent(GitModule.CurrentWorkingDir + file.Name, diffViewer.Encoding);
                }
                else
                {
                    firstRevision = secondRevision;
                    secondRevision = string.Empty;
                }
            }
            if (firstRevision == GitRevision.IndexGuid) //index
            {
                if (secondRevision == null)
                {
                    return ProcessDiffText(GitModule.Current.GetCurrentChanges(file.Name, file.OldName, true,
                        diffViewer.GetExtraDiffArguments(), diffViewer.Encoding), file.IsSubmodule);
                }

                //rev1 vs index
                firstRevision = secondRevision;
                secondRevision = string.Empty;
                extraDiffArgs = string.Join(" ", extraDiffArgs, "--cached");
            }

            Debug.Assert(!GitRevision.IsArtificial(firstRevision), string.Join(" ", firstRevision,secondRevision));                

            if (secondRevision == null)
                secondRevision = firstRevision + "^";            

            PatchApply.Patch patch = GitModule.Current.GetSingleDiff(firstRevision, secondRevision, file.Name, file.OldName,
                                                    string.Join(" ", diffViewer.GetExtraDiffArguments(), extraDiffArgs), diffViewer.Encoding);

            if (patch == null)
                return string.Empty;

            return ProcessDiffText(patch.Text, file.IsSubmodule);
        }

        private static string ProcessDiffText(string diff, bool isSubmodule)
        {
            if (isSubmodule)
                return GitCommandHelpers.ProcessSubmodulePatch(diff);

            return diff;
        }

        public static void ViewPatch(this FileViewer diffViewer, RevisionGrid grid, GitItemStatus file, string defaultText)
        {
            IList<GitRevision> revisions = grid.GetSelectedRevisions();

            if (revisions.Count == 1 && (revisions[0].ParentGuids == null || revisions[0].ParentGuids.Length == 0))
            {
                diffViewer.ViewGitItem(file.Name, file.TreeGuid);
            }
            else
            {
                diffViewer.ViewPatch(() =>
                                       {
                                           string selectedPatch = diffViewer.GetSelectedPatch(grid, file);
                                           return selectedPatch ?? defaultText;
                                       });
            }
        }

        public static void RemoveIfExists(this TabControl tabControl, TabPage page)
        {
            if (tabControl.TabPages.Contains(page))
                tabControl.TabPages.Remove(page);
        }

        public static void InsertIfNotExists(this TabControl tabControl, int index, TabPage page)
        {
            if (!tabControl.TabPages.Contains(page))
                tabControl.TabPages.Insert(index, page);
        }

        public static void Mask(this Control control)
        {
            if (control.FindMaskPanel() == null)
            {
                MaskPanel panel = new MaskPanel();
                control.Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                panel.BringToFront();
            }
        }

        public static void UnMask(this Control control)
        {
            MaskPanel panel = control.FindMaskPanel();
            if (panel != null)
            {
                control.Controls.Remove(panel);
                panel.Dispose();
            }
        }

        private static MaskPanel FindMaskPanel(this Control control)
        {
            foreach (var c in control.Controls)
                if (c is MaskPanel)
                    return c as MaskPanel;

            return null;
        }

        public class MaskPanel : PictureBox
        {
            public MaskPanel() 
            {
                Image = Properties.Resources.loadingpanel;
                SizeMode = PictureBoxSizeMode.CenterImage;
                BackColor = SystemColors.AppWorkspace;
            }
        }
    }
}

namespace Sitecore.Support.XA.Foundation.PlaceholderSettings.WebEdit.Dialogs.PlaceholderSettings
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Abstractions;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Data.Templates;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Pipelines;
    using Sitecore.Resources;
    using Sitecore.SecurityModel;
    using Sitecore.Shell.Applications.Dialogs.ItemLister;
    using Sitecore.Shell.Applications.WebEdit.Dialogs;
    using Sitecore.StringExtensions;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls;
    using Sitecore.XA.Foundation.Abstractions;
    using Sitecore.XA.Foundation.Multisite.Extensions;
    using Sitecore.XA.Foundation.PlaceholderSettings;
    using Sitecore.XA.Foundation.PlaceholderSettings.Model;
    using Sitecore.XA.Foundation.PlaceholderSettings.Pipelines.CreatePlaceholderSetting;
    using Sitecore.XA.Foundation.PlaceholderSettings.Pipelines.GetPlaceholderSetting;
    using Sitecore.XA.Foundation.Presentation;
    using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
    using Sitecore.XA.Foundation.SitecoreExtensions.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    public class PlaceholderSettingsForm : Sitecore.Shell.Applications.WebEdit.Dialogs.PlaceholderSettingsForm
    {
        private Literal CachingTitle;

        private Item _currentSettingItem;

        private Item _contextItem;

        private Template _templateForCreation;

        private Border Caching;

        private ID _cachingSectionId = ID.Parse("{64CA39AE-89D3-4A78-B1F9-9E907CAAE6BC}");

        protected BaseClient BaseClient => ServiceLocator.ServiceProvider.GetService<BaseClient>();

        private Item ContextItem
        {
            get
            {
                if (_contextItem == null)
                {
                    string text = ServerProperties["context_itemUri"] as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        _contextItem = Database.GetItem(new ItemUri(text));
                    }
                }
                return _contextItem;
            }
            set
            {
                Assert.IsNotNull(value, "value");
                _contextItem = value;
                ServerProperties["context_itemUri"] = value.Uri.ToString();
            }
        }

        private Item CurrentSettingItem
        {
            get
            {
                if (_currentSettingItem == null)
                {
                    string text = ServerProperties["current_settings"] as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        _currentSettingItem = Database.GetItem(new ItemUri(text));
                    }
                }
                return _currentSettingItem;
            }
            set
            {
                Assert.IsNotNull(value, "value");
                _currentSettingItem = value;
                ServerProperties["current_settings"] = value.Uri.ToString();
            }
        }

        private Template TemplateForCreation
        {
            get
            {
                if (_templateForCreation == null)
                {
                    object obj = ServerProperties["tmpl_creation"];
                    if (obj != null)
                    {
                        _templateForCreation = TemplateManager.GetTemplate(ID.Parse(obj), Client.ContentDatabase);
                    }
                }
                return _templateForCreation;
            }
            set
            {
                Assert.IsNotNull(value, "value");
                _templateForCreation = value;
                ServerProperties["tmpl_creation"] = value.ID.ToString();
            }
        }

        protected IContext Context
        {
            get;
        } = ServiceLocator.ServiceProvider.GetService<IContext>();


        protected TreeviewEx NewSettingTreeview
        {
            get;
            set;
        }

        private void SetInputControlsState(Checkbox checkbox, Listbox listbox, Button button)
        {
            Assert.ArgumentNotNull(checkbox, "checkbox");
            Assert.ArgumentNotNull(listbox, "listbox");
            Assert.ArgumentNotNull(button, "button");
            button.Disabled = !checkbox.Checked;
            listbox.Disabled = !checkbox.Checked;
        }

        private void RenderList(Listbox listbox, ListString items)
        {
            Assert.ArgumentNotNull(listbox, "listbox");
            Assert.ArgumentNotNull(items, "items");
            listbox.Controls.Clear();
            foreach (string item2 in items)
            {
                if (ID.TryParse(item2, out ID result))
                {
                    ListItem listItem = new ListItem();
                    listItem.ID = result.ToShortID() + listbox.ClientID;
                    Item item = Client.ContentDatabase.GetItem(result);
                    if (item != null)
                    {
                        listItem.Value = result.ToString();
                        listItem.Header = item.GetUIDisplayName();
                        listbox.Controls.Add(listItem);
                    }
                }
            }
        }

        protected virtual void SetCachingTitle()
        {
            CachingTitle.Text = string.Format("{0}:", Translate.Text("Caching"));
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            Parent.OnChanged += base.ParentChanged;
            if (Context.ClientPage.IsEvent)
            {
                return;
            }
            SetCachingTitle();
            SelectOption.Click = string.Format("ChangeMode(\"{0}\")", "Select");
            CreateOption.Click = string.Format("ChangeMode(\"{0}\")", "Create");
            EditControls.Click = $"EditAllowedControls_Click(\"{AllowedControls.ID}\")";
            ExtendedSelectPlaceholderSettingsOptions extendedSelectPlaceholderSettingsOptions = SelectItemOptions.Parse<ExtendedSelectPlaceholderSettingsOptions>();
            if (!string.IsNullOrEmpty(extendedSelectPlaceholderSettingsOptions.PlaceholderKey))
            {
                NewSettingsPlaceholderKey.Value = extendedSelectPlaceholderSettingsOptions.PlaceholderKey;
                PlaceholderKey.Value = extendedSelectPlaceholderSettingsOptions.PlaceholderKey;
                EditPlaceholderKey.Value = extendedSelectPlaceholderSettingsOptions.PlaceholderKey;
            }
            if (!extendedSelectPlaceholderSettingsOptions.IsPlaceholderKeyEditable)
            {
                NewSettingsPlaceholderKey.Disabled = true;
                PlaceholderKey.Disabled = true;
                EditPlaceholderKey.Disabled = true;
            }
            if (extendedSelectPlaceholderSettingsOptions.TemplateForCreating != null)
            {
                TemplateForCreation = extendedSelectPlaceholderSettingsOptions.TemplateForCreating;
                TemplateItem templateItem = Client.ContentDatabase.GetItem(TemplateForCreation.ID);
                if (templateItem != null && templateItem.StandardValues != null)
                {
                    CheckboxField checkboxField = templateItem.StandardValues.Fields["Editable"];
                    if (checkboxField != null)
                    {
                        Editable.Checked = checkboxField.Checked;
                        SetInputControlsState(Editable, AllowedControls, EditControls);
                    }
                }
                if (!string.IsNullOrEmpty(DataContext.Root))
                {
                    ParentDataContext.Root = DataContext.Root;
                    if (!string.IsNullOrEmpty(extendedSelectPlaceholderSettingsOptions.PlaceholderKey))
                    {
                        NewSettingsName.Value = GetNewItemDefaultName(Client.ContentDatabase.GetItem(ParentDataContext.Root), StringUtil.GetLastPart(extendedSelectPlaceholderSettingsOptions.PlaceholderKey, '/', extendedSelectPlaceholderSettingsOptions.PlaceholderKey));
                    }
                }
                ParentDataContext.DataViewName = DataContext.DataViewName;
            }
            else
            {
                CreateOption.Disabled = true;
                CreateOption.Class = "option-disabled";
                CreateOption.Click = "javascript:void(0);";
                CreateIcon.Src = Images.GetThemedImageSource(CreateIcon.Src, ImageDimension.id32x32, disabled: true);
                ParentDataContext.DataViewName = DataContext.DataViewName;
            }
            if (extendedSelectPlaceholderSettingsOptions.CurrentSettingsItem != null)
            {
                CurrentSettingItem = extendedSelectPlaceholderSettingsOptions.CurrentSettingsItem;
                EditOption.Visible = true;
                CurrentMode = "Edit";
                EditOption.Click = string.Format("ChangeMode(\"{0}\")", "Edit");
                InitEditingControls(extendedSelectPlaceholderSettingsOptions.CurrentSettingsItem);
                SetControlsOnModeChange();
                SelectedSettingsEditControls.Click = $"EditAllowedControls_Click(\"{SelectedSettingsAllowedControls.ID}\")";
            }
            else
            {
                SetControlsForSelection(DataContext.GetFolder());
            }
            RegisterStartupScripts();
            if (extendedSelectPlaceholderSettingsOptions.CurrentItemUri == null)
            {
                Item item = GetCurrentItemFromSession();
                if (item == null)
                {
                    item = GetCurrentItemFromRerquestUrl();
                }
                ContextItem = item;
            }
            else
            {
                ContextItem = Client.ContentDatabase.GetItem(extendedSelectPlaceholderSettingsOptions.CurrentItemUri.ItemID);
            }
            if (ContextItem.IsSxaSite())
            {
                AddTreeRoots(ContextItem);
                NewSettingTreeview.Visible = true;
                Parent.Visible = false;
                TemplateForCreation = TemplateManager.GetTemplate(ID.Parse(Sitecore.XA.Foundation.PlaceholderSettings.Templates.SxaPlaceholder.ID), Client.ContentDatabase);
            }
            RenderCachingFields(extendedSelectPlaceholderSettingsOptions.CurrentSettingsItem);
        }

        //fix of the issue 247421
        protected virtual Item GetCurrentItemFromRerquestUrl()
        {
            Item item = null;
            string queryString = System.Web.HttpContext.Current.Request.Url.Query;
            var parameters = WebUtil.ParseQueryString(queryString);
            if (parameters != null)
            {
                string key = "cdi";
                var parameterValue = WebUtil.ParseQueryString(queryString)[key];
                var decodedParameters = System.Web.HttpUtility.UrlDecode(parameterValue);
                if (decodedParameters != null)
                {
                    item = BaseClient.ContentDatabase.GetItem(decodedParameters);
                }

                if (item == null)
                {
                    key = "contextItemId";
                    var parametersValue = WebUtil.ParseQueryString(queryString)["parameters"];
                    decodedParameters = System.Web.HttpUtility.UrlDecode(parametersValue);
                    if (decodedParameters != null)
                    {
                        var safeDictionary = WebUtil.ParseQueryString(decodedParameters);
                        if (safeDictionary.ContainsKey(key))
                        {
                            var idRaw = safeDictionary[key];
                            if (!string.IsNullOrWhiteSpace(idRaw) && ID.TryParse(idRaw, out ID id))
                            {
                                item = BaseClient.ContentDatabase.GetItem(id);
                            }
                        }
                    }
                }
            }
            return item;
        }
        //end of the fix

        protected virtual void RenderCachingFields(Item currentSettingsItem)
        {
            if (currentSettingsItem == null || !currentSettingsItem.InheritsFrom(Sitecore.XA.Foundation.Presentation.Templates.Caching.ID))
            {
                return;
            }
            Template template = TemplateManager.GetTemplate(Sitecore.XA.Foundation.Presentation.Templates.Caching.ID, currentSettingsItem.Database);
            if (template == null)
            {
                return;
            }
            Item item = ContextItem.Database.GetItem(_cachingSectionId);
            if (item != null)
            {
                CheckboxField checkboxField = currentSettingsItem.Fields[Sitecore.XA.Foundation.Presentation.Templates.Caching.Fields.ResetCachingOptions];
                if (checkboxField != null)
                {
                    RenderCheckbox(checkboxField.InnerField.Title, checkboxField.InnerField.ID.ToString(), checkboxField.Checked, "resetoptions");
                }
                foreach (Item child in item.Children)
                {
                    TemplateField field = template.GetField(child.ID);
                    if (field != null)
                    {
                        Field field2 = currentSettingsItem.Fields[field.ID];
                        if (field2 != null)
                        {
                            CheckboxField checkboxField2 = new CheckboxField(field2);
                            RenderCheckbox(checkboxField2.InnerField.Title, field.ID.ToString(), checkboxField2.Checked, $"field-{checkboxField2.InnerField.Key} caching-field");
                        }
                    }
                }
            }
        }

        protected virtual void RenderCheckbox(string name, string id, bool selected, string cls)
        {
            Border border = new Border();
            border.Attributes.Add("class", cls);
            Checkbox checkbox = new Checkbox();
            Label label = new Label();
            label.Header = name;
            checkbox.ID = $"cachingFields_{id}";
            border.Controls.Add(checkbox);
            border.Controls.Add(label);
            checkbox.Checked = selected;
            Caching.Controls.Add(border);
        }

        protected virtual void SaveCachingOptions()
        {
            Item currentSettingItem = CurrentSettingItem;
            if (currentSettingItem == null)
            {
                SheerResponse.Alert(Translate.Text("Item \"{0}\" not found."));
                return;
            }
            currentSettingItem.Editing.BeginEdit();
            foreach (Checkbox item in (from control in WebUtil.FindSubControls<Control>(Caching)
                                       where control is Checkbox
                                       select control).Cast<Checkbox>())
            {
                string text = item.ID.Replace("cachingFields_", string.Empty);
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                CheckboxField checkboxField = currentSettingItem.Fields[text];
                if (checkboxField != null)
                {
                    checkboxField.Checked = item.Checked;
                }
            }
            currentSettingItem.Editing.EndEdit();
        }

        protected virtual Item GetCurrentItemFromSession()
        {
            string key = "id";
            ClientPipelineArgs suspendedPipelineArgs = ServiceLocator.ServiceProvider.GetService<ISuspendedPipelineService>().GetSuspendedPipelineArgs((ClientPipelineArgs args) => args.Parameters.AllKeys.Contains(key));
            if (suspendedPipelineArgs != null)
            {
                return Client.ContentDatabase.GetItem(new ID(suspendedPipelineArgs.Parameters[key]));
            }
            return null;
        }

        private void AddTreeRoots(Item contextItem)
        {
            int num = 0;
            ListString listString = new ListString();
            GetPlaceholderSettingArgs getPlaceholderSettingArgs = new GetPlaceholderSettingArgs
            {
                ContextItem = contextItem
            };
            CorePipeline.Run("getPlaceholderSetting", getPlaceholderSettingArgs, failIfNotExists: false);
            foreach (Item settingsRoot in getPlaceholderSettingArgs.SettingsRoots)
            {
                string text = "DataContext" + num;
                DataContext dataContext = CopyDataContext(DataContext, text);
                dataContext.Root = settingsRoot.Paths.FullPath;
                if (CurrentSettingItem != null)
                {
                    dataContext.DefaultItem = CurrentSettingItem.Paths.FullPath;
                    if (CurrentSettingItem.Paths.Path.StartsWith(settingsRoot.Paths.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        MultiRootTreeview multiRootTreeview = Treeview as MultiRootTreeview;
                        if (multiRootTreeview != null)
                        {
                            multiRootTreeview.CurrentDataContext = text;
                        }
                    }
                }
                Context.ClientPage.AddControl(Dialog, dataContext);
                listString.Add(dataContext.ID);
                num++;
            }
            Treeview.DataContext = listString.ToString();
            NewSettingTreeview.DataContext = listString.ToString();
        }

        protected virtual DataContext CopyDataContext(DataContext dataContext, string id)
        {
            Assert.ArgumentNotNull(dataContext, "dataContext");
            Assert.ArgumentNotNull(id, "id");
            return new DataContext
            {
                Filter = dataContext.Filter,
                DataViewName = dataContext.DataViewName,
                ID = id
            };
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            string currentMode = CurrentMode;
            if (!(currentMode == "Edit"))
            {
                if (!(currentMode == "Select"))
                {
                    if (currentMode == "Create")
                    {
                        Item item = CreateNewSettings();
                        if (item != null)
                        {
                            SheerResponse.SetDialogValue(item.ID + "|" + NewSettingsPlaceholderKey.Value);
                            SheerResponse.CloseWindow();
                        }
                    }
                    return;
                }
                Item selectionItem = Treeview.GetSelectionItem();
                if (selectionItem != null)
                {
                    if (PlaceholderKey.Value.Trim().Length == 0)
                    {
                        SheerResponse.Alert(Translate.Text("Specify a placeholder name."));
                        return;
                    }
                    SheerResponse.SetDialogValue(selectionItem.ID + "|" + PlaceholderKey.Value);
                    SheerResponse.CloseWindow();
                }
            }
            else if (EditExistingSetting() != null)
            {
                SaveCachingOptions();
                SheerResponse.SetDialogValue("#currentmodified#|" + EditPlaceholderKey.Value);
                SheerResponse.CloseWindow();
            }
        }

        protected void NewSettingTreeview_Click()
        {
            SetControlsForSelectionForNewSetting(NewSettingTreeview.GetSelectionItem());
        }

        private Item CreateNewSettings()
        {
            string text = GetCreateSettingSelectionItem().ID.ToString();
            Assert.IsNotNullOrEmpty(text, "itemPath");
            string value = NewSettingsName.Value;
            CreatePlaceholderSettingGetArgs createPlaceholderSettingGetArgs = new CreatePlaceholderSettingGetArgs
            {
                ContentDatabase = Client.ContentDatabase,
                Path = text,
                Name = value,
                PlaceholderKey = NewSettingsPlaceholderKey.Value,
                TemplateForCreation = TemplateForCreation,
                ContextItem = ContextItem
            };
            createPlaceholderSettingGetArgs.PlaceholderSettingFields = new Dictionary<string, string>
        {
            {
                "Editable",
                Editable.Checked ? "1" : "0"
            },
            {
                "Allowed Controls",
                GetAllowedContros(AllowedControls).ToString()
            }
        };
            CorePipeline.Run("createPlaceholderSetting", createPlaceholderSettingGetArgs);
            if (!createPlaceholderSettingGetArgs.Aborted)
            {
                return createPlaceholderSettingGetArgs.PlaceholderSettingItem;
            }
            return null;
        }

        protected virtual Item GetCreateSettingSelectionItem()
        {
            if (ContextItem.IsSxaSite())
            {
                return NewSettingTreeview.GetSelectionItem();
            }
            return Client.ContentDatabase.GetItem(Parent.Value);
        }

        private Item EditExistingSetting()
        {
            Item currentSettingItem = CurrentSettingItem;
            if (currentSettingItem == null)
            {
                SheerResponse.Alert(Translate.Text("Item \"{0}\" not found."));
                return null;
            }
            if (EditPlaceholderKey.Value.Trim().Length == 0)
            {
                SheerResponse.Alert(Translate.Text("Specify a placeholder name."));
                return null;
            }
            currentSettingItem.Editing.BeginEdit();
            ((CheckboxField)currentSettingItem.Fields["Editable"]).Checked = SelectedSettingsEditable.Checked;
            currentSettingItem.Fields["Allowed Controls"].Value = GetAllowedContros(SelectedSettingsAllowedControls).ToString();
            currentSettingItem.Editing.EndEdit();
            return currentSettingItem;
        }

        private ListString GetAllowedContros(Listbox control)
        {
            Assert.ArgumentNotNull(control, "control");
            return new ListString((from i in control.Items
                                   select ShortID.Decode(StringUtil.Left(i.ID, 32)).ToString()).ToList());
        }

        private void RegisterStartupScripts()
        {
            string text = null;
            if (CurrentMode == "Edit" && !EditPlaceholderKey.Disabled)
            {
                text = EditPlaceholderKey.ClientID;
            }
            if (CurrentMode == "Select" && !PlaceholderKey.Disabled)
            {
                text = PlaceholderKey.ClientID;
            }
            if (text != null)
            {
                Context.ClientPage.ClientScript.RegisterStartupScript(Context.ClientPage.GetType(), "startScript", $"selectValue('{text}');", addScriptTags: true);
            }
        }

        protected override void SetControlsOnModeChange()
        {
            base.SetControlsOnModeChange();
            string currentMode = CurrentMode;
            if (!(currentMode == "Edit"))
            {
                if (!(currentMode == "Select"))
                {
                    if (currentMode == "Create")
                    {
                        SelectSection.Visible = false;
                        CreateSection.Visible = true;
                        EditSection.Visible = false;
                        if (EditOption.Visible)
                        {
                            EditOption.Class = string.Empty;
                        }
                        SetControlsForSelectionForNewSetting(GetCreateSettingSelectionItem());
                        SectionHeader.Text = Translate.Text("Create a new placeholder settings item.");
                        SheerResponse.Eval($"selectValue('{NewSettingsName.ClientID}')");
                    }
                }
                else
                {
                    SelectSection.Visible = true;
                    EditSection.Visible = false;
                    CreateSection.Visible = false;
                    if (EditOption.Visible)
                    {
                        EditOption.Class = string.Empty;
                    }
                    SetControlsForSelection(Treeview.GetSelectionItem());
                    SectionHeader.Text = Translate.Text("Select an existing placeholder settings item.");
                    if (!PlaceholderKey.Disabled)
                    {
                        SheerResponse.Eval($"selectValue('{PlaceholderKey.ClientID}')");
                    }
                }
            }
            else
            {
                EditSection.Visible = true;
                CreateSection.Visible = false;
                SelectSection.Visible = false;
                EditOption.Class = "selected";
                SelectOption.Class = string.Empty;
                if (!CreateOption.Disabled)
                {
                    CreateOption.Class = string.Empty;
                }
                SetControlsForEditing();
                SectionHeader.Text = Translate.Text("Edit the selected placeholder settings item.");
                if (!EditPlaceholderKey.Disabled)
                {
                    SheerResponse.Eval($"selectValue('{EditPlaceholderKey.ClientID}')");
                }
            }
        }

        private void InitEditingControls(Item settingsItem)
        {
            Assert.IsNotNull(settingsItem, "settingsItem");
            SelectedSettingName.Text = settingsItem.GetUIDisplayName();
            SelectedSettingName.ToolTip = settingsItem.Paths.FullPath;
            CheckboxField checkboxField = settingsItem.Fields["Editable"];
            if (checkboxField != null)
            {
                SelectedSettingsEditable.Checked = checkboxField.Checked;
                SetInputControlsState(SelectedSettingsEditable, SelectedSettingsAllowedControls, SelectedSettingsEditControls);
            }
            string text = settingsItem["Allowed Controls"];
            if (!string.IsNullOrEmpty(text))
            {
                RenderList(SelectedSettingsAllowedControls, new ListString(text));
            }
        }

        private void SetControlsForEditing()
        {
            Item currentSettingItem = CurrentSettingItem;
            Warnings.Visible = false;
            if (currentSettingItem == null)
            {
                OK.Disabled = true;
                return;
            }
            if (!currentSettingItem.Access.CanWrite())
            {
                OK.Disabled = true;
                Information.Text = Translate.Text("You cannot edit this item because you do not have write access to it.");
                Warnings.Visible = true;
                return;
            }
            string text = currentSettingItem["Placeholder Key"];
            if (!string.IsNullOrEmpty(text))
            {
                Information.Text = Translate.Text("The settings affect all placeholders with '{0}' key.").FormatWith($"<span title=\"{text}\">{StringUtil.Clip(text, 25, ellipsis: true)}</span>");
                Warnings.Visible = true;
            }
            OK.Disabled = false;
        }

        private void SetControlsForSelection(Item item)
        {
            Warnings.Visible = false;
            if (!Policy.IsAllowed("Page Editor/Can Select Placeholder Settings"))
            {
                OK.Disabled = true;
                Information.Text = Translate.Text("The action cannot be executed because of security restrictions.");
                Warnings.Visible = true;
            }
            else if (item != null)
            {
                if (!IsSelectable(item))
                {
                    OK.Disabled = true;
                    Information.Text = Translate.Text("The '{0}' item is not a valid selection.").FormatWith(StringUtil.Clip(item.GetUIDisplayName(), 20, ellipsis: true));
                    Warnings.Visible = true;
                }
                else
                {
                    Information.Text = string.Empty;
                    OK.Disabled = false;
                }
            }
        }

        private void SetControlsForSelectionForNewSetting(Item item)
        {
            Warnings.Visible = false;
            if (!Policy.IsAllowed("Page Editor/Can Select Placeholder Settings"))
            {
                OK.Disabled = true;
                Information.Text = Translate.Text("The action cannot be executed because of security restrictions.");
                Warnings.Visible = true;
            }
            else if (item != null)
            {
                if (!GetSelectableTempalteIds().Any((Func<ID, bool>)item.InheritsFrom))
                {
                    OK.Disabled = true;
                    Information.Text = Translate.Text("The '{0}' item is not a valid selection.").FormatWith(StringUtil.Clip(item.GetUIDisplayName(), 20, ellipsis: true));
                    Warnings.Visible = true;
                }
                else
                {
                    Information.Text = string.Empty;
                    OK.Disabled = false;
                }
            }
        }

        protected virtual IEnumerable<ID> GetSelectableTempalteIds()
        {
            return from XmlNode node in Factory.GetConfigNodes("experienceAccelerator/placeholderSettings/selectablePlaceholderSettingRootTemplates/templateID")
                   select new ID(node.InnerText);
        }
    }
}
namespace Sitecore.Support.Shell.Applications.Layouts.DeviceEditor
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Databases;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Layouts;
    using Sitecore.Pipelines.RenderDeviceEditorRendering;
    using Sitecore.Resources;
    using Sitecore.Rules;
    using Sitecore.SecurityModel;
    using Sitecore.Shell.Applications.Dialogs;
    using Sitecore.Shell.Applications.Dialogs.ItemLister;
    using Sitecore.Shell.Applications.Dialogs.Personalize;
    using Sitecore.Shell.Applications.Dialogs.Testing;
    using Sitecore.Shell.Applications.Layouts.DeviceEditor;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.XmlControls;
    using System;
    using System.Collections;
    using System.Linq;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;

    /// <summary>
    /// Represents the Device Editor form.
    /// </summary>
    [UsedImplicitly]
    public class DeviceEditorForm : DialogForm
    {
        /// <summary>
        ///   The command name.
        /// </summary>
        private const string CommandName = "device:settestdetails";

        /// <summary>
        /// The instance of <see cref="P:Sitecore.Shell.Applications.Layouts.DeviceEditor.DeviceEditorForm.DatabaseHelper" />.
        /// </summary>
        protected DatabaseHelper DatabaseHelper
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the controls.
        /// </summary>
        /// <value>The controls.</value>
        public ArrayList Controls
        {
            get
            {
                return (ArrayList)Context.ClientPage.ServerProperties["Controls"];
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                Context.ClientPage.ServerProperties["Controls"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the device ID.
        /// </summary>
        /// <value>The device ID.</value>
        public string DeviceID
        {
            get
            {
                return StringUtil.GetString(Context.ClientPage.ServerProperties["DeviceID"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                Context.ClientPage.ServerProperties["DeviceID"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected.
        /// </summary>
        /// <value>The index of the selected.</value>
        public int SelectedIndex
        {
            get
            {
                return MainUtil.GetInt(Context.ClientPage.ServerProperties["SelectedIndex"], -1);
            }
            set
            {
                Context.ClientPage.ServerProperties["SelectedIndex"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public string UniqueId
        {
            get
            {
                return StringUtil.GetString(Context.ClientPage.ServerProperties["PlaceholderUniqueID"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                Context.ClientPage.ServerProperties["PlaceholderUniqueID"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the layout.
        /// </summary>
        /// <value>The layout.</value>
        protected TreePicker Layout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the placeholders.
        /// </summary>
        /// <value>The placeholders.</value>
        protected Scrollbox Placeholders
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the renderings.
        /// </summary>
        /// <value>The renderings.</value>
        protected Scrollbox Renderings
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the test.
        /// </summary>
        /// <value>The test button.</value>
        protected Button Test
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the personalize button control.
        /// </summary>
        /// <value>The personalize button control.</value>
        protected Button Personalize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the edit.
        /// </summary>
        /// <value>The edit button.</value>
        protected Button btnEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change button.</value>
        protected Button btnChange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the remove.
        /// </summary>
        /// <value>The Remove button.</value>
        protected Button btnRemove
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the move up.
        /// </summary>
        /// <value>The Move Up button.</value>
        protected Button MoveUp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the move down.
        /// </summary>
        /// <value>The Move Down button.</value>
        protected Button MoveDown
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Edit placeholder button.
        /// </summary>
        /// <value>The Edit placeholder button.</value>
        protected Button phEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the phRemove button.
        /// </summary>
        /// <value>Remove place holder button.</value>
        protected Button phRemove
        {
            get;
            set;
        }

        /// <summary>
        /// Initialize new instance of <see cref="T:Sitecore.Shell.Applications.Layouts.DeviceEditor.DeviceEditorForm" />.
        /// </summary>
        public DeviceEditorForm()
        {
            DatabaseHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Adds the specified arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:add", true)]
        [UsedImplicitly]
        protected void Add(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                {
                    return;
                }
                string[] array = args.Result.Split(',');
                string text = array[0];
                string placeholder = array[1].Replace("-c-", ",");
                bool num = array[2] == "1";
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                DeviceDefinition device = layoutDefinition.GetDevice(DeviceID);
                RenderingDefinition renderingDefinition = new RenderingDefinition
                {
                    ItemID = text,
                    Placeholder = placeholder
                };
                device.AddRendering(renderingDefinition);
                SetDefinition(layoutDefinition);
                Refresh();
                if (num)
                {
                    ArrayList renderings = device.Renderings;
                    if (renderings != null)
                    {
                        SelectedIndex = renderings.Count - 1;
                        Context.ClientPage.SendMessage(this, "device:edit");
                    }
                }
                Registry.SetString("/Current_User/SelectRendering/Selected", text);
            }
            else
            {
                SelectRenderingOptions selectRenderingOptions = new SelectRenderingOptions
                {
                    ShowOpenProperties = true,
                    ShowPlaceholderName = true,
                    PlaceholderName = string.Empty
                };
                string @string = Registry.GetString("/Current_User/SelectRendering/Selected");
                if (!string.IsNullOrEmpty(@string))
                {
                    selectRenderingOptions.SelectedItem = Client.ContentDatabase.GetItem(@string);
                }
                SheerResponse.ShowModalDialog(selectRenderingOptions.ToUrlString(Client.ContentDatabase).ToString(), response: true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Adds the specified arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:addplaceholder", true)]
        [UsedImplicitly]
        protected void AddPlaceholder(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    LayoutDefinition layoutDefinition = GetLayoutDefinition();
                    DeviceDefinition device = layoutDefinition.GetDevice(DeviceID);
                    string placeholderKey;
                    Item item = SelectPlaceholderSettingsOptions.ParseDialogResult(args.Result, Client.ContentDatabase, out placeholderKey);
                    if (item != null && !string.IsNullOrEmpty(placeholderKey))
                    {
                        PlaceholderDefinition placeholderDefinition = new PlaceholderDefinition
                        {
                            UniqueId = ID.NewID.ToString(),
                            MetaDataItemId = item.ID.ToString(),
                            Key = placeholderKey
                        };
                        device.AddPlaceholder(placeholderDefinition);
                        SetDefinition(layoutDefinition);
                        Refresh();
                    }
                }
            }
            else
            {
                var options = new SelectPlaceholderSettingsOptions
                {
                    IsPlaceholderKeyEditable = true
                };
                var queryString = WebUtil.ParseQueryString(System.Web.HttpContext.Current.Request.Url.Query);
                if (queryString.ContainsKey("id"))
                {
                    string value = System.Web.HttpUtility.UrlDecode(queryString["id"]);
                    string key = "contextItemId";
                    options.Parameters = $"{key}={value}";
                }
                SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "460px", "460px", string.Empty, response: true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Adds the specified arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:change", true)]
        [UsedImplicitly]
        protected void Change(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SelectedIndex < 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            ArrayList renderings = layoutDefinition.GetDevice(DeviceID).Renderings;
            if (renderings == null)
            {
                return;
            }
            RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
            if (renderingDefinition == null || string.IsNullOrEmpty(renderingDefinition.ItemID))
            {
                return;
            }
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    string[] array = args.Result.Split(',');
                    renderingDefinition.ItemID = array[0];
                    bool num = array[2] == "1";
                    SetDefinition(layoutDefinition);
                    Refresh();
                    if (num)
                    {
                        Context.ClientPage.SendMessage(this, "device:edit");
                    }
                }
            }
            else
            {
                SheerResponse.ShowModalDialog(new SelectRenderingOptions
                {
                    ShowOpenProperties = true,
                    ShowPlaceholderName = false,
                    PlaceholderName = string.Empty,
                    SelectedItem = Client.ContentDatabase.GetItem(renderingDefinition.ItemID)
                }.ToUrlString(Client.ContentDatabase).ToString(), response: true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Edits the specified arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:edit", true)]
        [UsedImplicitly]
        protected void Edit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (new RenderingParameters
            {
                Args = args,
                DeviceId = DeviceID,
                SelectedIndex = SelectedIndex,
                Item = UIUtil.GetItemFromQueryString(Client.ContentDatabase)
            }.Show())
            {
                Refresh();
            }
        }

        /// <summary>
        /// Edits the placeholder.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:editplaceholder", true)]
        [UsedImplicitly]
        protected void EditPlaceholder(ClientPipelineArgs args)
        {
            if (string.IsNullOrEmpty(UniqueId))
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            PlaceholderDefinition placeholder = layoutDefinition.GetDevice(DeviceID).GetPlaceholder(UniqueId);
            if (placeholder == null)
            {
                return;
            }
            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    string placeholderKey;
                    Item item = SelectPlaceholderSettingsOptions.ParseDialogResult(args.Result, Client.ContentDatabase, out placeholderKey);
                    if (item != null)
                    {
                        placeholder.MetaDataItemId = item.Paths.FullPath;
                        placeholder.Key = placeholderKey;
                        SetDefinition(layoutDefinition);
                        Refresh();
                    }
                }
            }
            else
            {
                Item itemByPathOrId = DatabaseHelper.GetItemByPathOrId(Client.ContentDatabase, placeholder.MetaDataItemId);
                SheerResponse.ShowModalDialog(new SelectPlaceholderSettingsOptions
                {
                    TemplateForCreating = null,
                    PlaceholderKey = placeholder.Key,
                    CurrentSettingsItem = itemByPathOrId,
                    SelectedItem = itemByPathOrId,
                    IsPlaceholderKeyEditable = true
                }.ToUrlString().ToString(), "460px", "460px", string.Empty, response: true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// The set test
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:test", true)]
        [UsedImplicitly]
        protected void SetTest(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SelectedIndex < 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            DeviceDefinition device = layoutDefinition.GetDevice(DeviceID);
            ArrayList renderings = device.Renderings;
            if (renderings == null)
            {
                return;
            }
            RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
            if (renderingDefinition == null)
            {
                return;
            }
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                {
                    return;
                }
                if (args.Result == "#reset#")
                {
                    renderingDefinition.MultiVariateTest = string.Empty;
                    SetDefinition(layoutDefinition);
                    Refresh();
                    return;
                }
                ID iD = SetTestDetailsOptions.ParseDialogResult(args.Result);
                if (ID.IsNullOrEmpty(iD))
                {
                    SheerResponse.Alert("Item not found.");
                    return;
                }
                renderingDefinition.MultiVariateTest = iD.ToString();
                SetDefinition(layoutDefinition);
                Refresh();
            }
            else
            {
                Command command = CommandManager.GetCommand("device:settestdetails", assert: true);
                CommandContext commandContext = new CommandContext();
                commandContext.Parameters["deviceDefinitionId"] = device.ID;
                commandContext.Parameters["renderingDefinitionUniqueId"] = renderingDefinition.UniqueId;
                command.Execute(commandContext);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page life cycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client post back,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                DeviceID = WebUtil.GetQueryString("de");
                DeviceDefinition device = GetLayoutDefinition().GetDevice(DeviceID);
                if (device.Layout != null)
                {
                    Layout.Value = device.Layout;
                }
                Personalize.Visible = Policy.IsAllowed("Page Editor/Extended features/Personalization");
                Command command = CommandManager.GetCommand("device:settestdetails", assert: true);
                Test.Visible = (command != null && command.QueryState(CommandContext.Empty) != CommandState.Hidden);
                Refresh();
                SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The arguments.
        /// </param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (Layout.Value.Length > 0)
            {
                Item item = Client.ContentDatabase.GetItem(Layout.Value);
                if (item == null)
                {
                    Context.ClientPage.ClientResponse.Alert("Layout not found.");
                    return;
                }
                if (item.TemplateID == TemplateIDs.Folder || item.TemplateID == TemplateIDs.Node)
                {
                    Context.ClientPage.ClientResponse.Alert(Translate.Text("\"{0}\" is not a layout.", item.GetUIDisplayName()));
                    return;
                }
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            DeviceDefinition device = layoutDefinition.GetDevice(DeviceID);
            ArrayList renderings = device.Renderings;
            if (renderings != null && renderings.Count > 0 && Layout.Value.Length == 0)
            {
                Context.ClientPage.ClientResponse.Alert("You must specify a layout when you specify renderings.");
                return;
            }
            device.Layout = Layout.Value;
            SetDefinition(layoutDefinition);
            Context.ClientPage.ClientResponse.SetDialogValue("yes");
            base.OnOK(sender, args);
        }

        /// <summary>
        /// Called when the rendering has click.
        /// </summary>
        /// <param name="uniqueId">
        /// The unique Id.
        /// </param>
        [UsedImplicitly]
        protected void OnPlaceholderClick(string uniqueId)
        {
            Assert.ArgumentNotNullOrEmpty(uniqueId, "uniqueId");
            if (!string.IsNullOrEmpty(UniqueId))
            {
                SheerResponse.SetStyle("ph_" + ID.Parse(UniqueId).ToShortID(), "background", string.Empty);
            }
            UniqueId = uniqueId;
            if (!string.IsNullOrEmpty(uniqueId))
            {
                SheerResponse.SetStyle("ph_" + ID.Parse(uniqueId).ToShortID(), "background", "#D0EBF6");
            }
            UpdatePlaceholdersCommandsState();
        }

        /// <summary>
        /// Called when the rendering has click.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        [UsedImplicitly]
        protected void OnRenderingClick(string index)
        {
            Assert.ArgumentNotNull(index, "index");
            if (SelectedIndex >= 0)
            {
                SheerResponse.SetStyle(StringUtil.GetString(Controls[SelectedIndex]), "background", string.Empty);
            }
            SelectedIndex = MainUtil.GetInt(index, -1);
            if (SelectedIndex >= 0)
            {
                SheerResponse.SetStyle(StringUtil.GetString(Controls[SelectedIndex]), "background", "#D0EBF6");
            }
            UpdateRenderingsCommandsState();
        }

        /// <summary>
        /// Personalizes the selected control.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [HandleMessage("device:personalize", true)]
        [UsedImplicitly]
        protected void PersonalizeControl(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SelectedIndex < 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            ArrayList renderings = layoutDefinition.GetDevice(DeviceID).Renderings;
            if (renderings == null)
            {
                return;
            }
            RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
            if (renderingDefinition == null || string.IsNullOrEmpty(renderingDefinition.ItemID) || string.IsNullOrEmpty(renderingDefinition.UniqueId))
            {
                return;
            }
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    XElement xElement2 = renderingDefinition.Rules = XElement.Parse(args.Result);
                    SetDefinition(layoutDefinition);
                    Refresh();
                }
            }
            else
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Client.ContentDatabase);
                string contextItemUri = (itemFromQueryString != null) ? itemFromQueryString.Uri.ToString() : string.Empty;
                SheerResponse.ShowModalDialog(new PersonalizeOptions
                {
                    SessionHandle = GetSessionHandle(),
                    DeviceId = DeviceID,
                    RenderingUniqueId = renderingDefinition.UniqueId,
                    ContextItemUri = contextItemUri
                }.ToUrlString().ToString(), "980px", "712px", string.Empty, response: true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Removes the specified message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        [HandleMessage("device:remove")]
        [UsedImplicitly]
        protected void Remove(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            int selectedIndex = SelectedIndex;
            if (selectedIndex < 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            ArrayList renderings = layoutDefinition.GetDevice(DeviceID).Renderings;
            if (renderings != null && selectedIndex >= 0 && selectedIndex < renderings.Count)
            {
                renderings.RemoveAt(selectedIndex);
                if (selectedIndex >= 0)
                {
                    SelectedIndex--;
                }
                SetDefinition(layoutDefinition);
                Refresh();
            }
        }

        /// <summary>
        /// Removes the placeholder.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        [HandleMessage("device:removeplaceholder")]
        [UsedImplicitly]
        protected void RemovePlaceholder(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (!string.IsNullOrEmpty(UniqueId))
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                DeviceDefinition device = layoutDefinition.GetDevice(DeviceID);
                PlaceholderDefinition placeholder = device.GetPlaceholder(UniqueId);
                if (placeholder != null)
                {
                    device.Placeholders?.Remove(placeholder);
                    SetDefinition(layoutDefinition);
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Sorts the down.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        [HandleMessage("device:sortdown")]
        [UsedImplicitly]
        protected void SortDown(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (SelectedIndex < 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            ArrayList renderings = layoutDefinition.GetDevice(DeviceID).Renderings;
            if (renderings != null && SelectedIndex < renderings.Count - 1)
            {
                RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
                if (renderingDefinition != null)
                {
                    renderings.Remove(renderingDefinition);
                    renderings.Insert(SelectedIndex + 1, renderingDefinition);
                    SelectedIndex++;
                    SetDefinition(layoutDefinition);
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Sorts the up.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        [HandleMessage("device:sortup")]
        [UsedImplicitly]
        protected void SortUp(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (SelectedIndex <= 0)
            {
                return;
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            ArrayList renderings = layoutDefinition.GetDevice(DeviceID).Renderings;
            if (renderings != null)
            {
                RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
                if (renderingDefinition != null)
                {
                    renderings.Remove(renderingDefinition);
                    renderings.Insert(SelectedIndex - 1, renderingDefinition);
                    SelectedIndex--;
                    SetDefinition(layoutDefinition);
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Gets the layout definition.
        /// </summary>
        /// <returns>
        /// The layout definition.
        /// </returns>
        /// <contract><ensures condition="not null" /></contract>
        private static LayoutDefinition GetLayoutDefinition()
        {
            string sessionString = WebUtil.GetSessionString(GetSessionHandle());
            Assert.IsNotNull(sessionString, "layout definition");
            return LayoutDefinition.Parse(sessionString);
        }

        /// <summary>
        /// Gets the session handle.
        /// </summary>
        /// <returns>
        /// The session handle string.
        /// </returns>
        private static string GetSessionHandle()
        {
            return "SC_DEVICEEDITOR";
        }

        /// <summary>
        /// Sets the definition.
        /// </summary>
        /// <param name="layout">
        /// The layout.
        /// </param>
        private static void SetDefinition(LayoutDefinition layout)
        {
            Assert.ArgumentNotNull(layout, "layout");
            string value = layout.ToXml();
            WebUtil.SetSessionValue(GetSessionHandle(), value);
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        private void Refresh()
        {
            Renderings.Controls.Clear();
            Placeholders.Controls.Clear();
            Controls = new ArrayList();
            DeviceDefinition device = GetLayoutDefinition().GetDevice(DeviceID);
            if (device.Renderings == null)
            {
                SheerResponse.SetOuterHtml("Renderings", Renderings);
                SheerResponse.SetOuterHtml("Placeholders", Placeholders);
                SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
                return;
            }
            int selectedIndex = SelectedIndex;
            RenderRenderings(device, selectedIndex, 0);
            RenderPlaceholders(device);
            UpdateRenderingsCommandsState();
            UpdatePlaceholdersCommandsState();
            SheerResponse.SetOuterHtml("Renderings", Renderings);
            SheerResponse.SetOuterHtml("Placeholders", Placeholders);
            SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
        }

        /// <summary>
        /// Renders the placeholders.
        /// </summary>
        /// <param name="deviceDefinition">
        /// The device definition.
        /// </param>
        private void RenderPlaceholders(DeviceDefinition deviceDefinition)
        {
            Assert.ArgumentNotNull(deviceDefinition, "deviceDefinition");
            ArrayList placeholders = deviceDefinition.Placeholders;
            if (placeholders != null)
            {
                foreach (PlaceholderDefinition item2 in placeholders)
                {
                    Item item = null;
                    string metaDataItemId = item2.MetaDataItemId;
                    if (!string.IsNullOrEmpty(metaDataItemId))
                    {
                        item = DatabaseHelper.GetItemByPathOrId(Client.ContentDatabase, metaDataItemId);
                    }
                    XmlControl xmlControl = Resource.GetWebControl("DeviceRendering") as XmlControl;
                    Assert.IsNotNull(xmlControl, typeof(XmlControl));
                    Placeholders.Controls.Add(xmlControl);
                    ID iD = ID.Parse(item2.UniqueId);
                    if (item2.UniqueId == UniqueId)
                    {
                        xmlControl["Background"] = "#D0EBF6";
                    }
                    string text2 = (string)(xmlControl["ID"] = "ph_" + iD.ToShortID());
                    xmlControl["Header"] = item2.Key;
                    xmlControl["Click"] = "OnPlaceholderClick(\"" + item2.UniqueId + "\")";
                    xmlControl["DblClick"] = "device:editplaceholder";
                    if (item != null)
                    {
                        xmlControl["Icon"] = item.Appearance.Icon;
                    }
                    else
                    {
                        xmlControl["Icon"] = "Imaging/24x24/layer_blend.png";
                    }
                }
            }
        }

        /// <summary>
        /// Renders the specified device definition.
        /// </summary>
        /// <param name="deviceDefinition">
        /// The device definition.
        /// </param>
        /// <param name="selectedIndex">
        /// Index of the selected.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        private void RenderRenderings(DeviceDefinition deviceDefinition, int selectedIndex, int index)
        {
            Assert.ArgumentNotNull(deviceDefinition, "deviceDefinition");
            ArrayList renderings = deviceDefinition.Renderings;
            if (renderings != null)
            {
                foreach (RenderingDefinition item2 in renderings)
                {
                    if (item2.ItemID != null)
                    {
                        Item item = Client.ContentDatabase.GetItem(item2.ItemID);
                        XmlControl xmlControl = Resource.GetWebControl("DeviceRendering") as XmlControl;
                        Assert.IsNotNull(xmlControl, typeof(XmlControl));
                        HtmlGenericControl htmlGenericControl = new HtmlGenericControl("div");
                        htmlGenericControl.Style.Add("padding", "0");
                        htmlGenericControl.Style.Add("margin", "0");
                        htmlGenericControl.Style.Add("border", "0");
                        htmlGenericControl.Style.Add("position", "relative");
                        htmlGenericControl.Controls.Add(xmlControl);
                        string uniqueID = Control.GetUniqueID("R");
                        Renderings.Controls.Add(htmlGenericControl);
                        htmlGenericControl.ID = Control.GetUniqueID("C");
                        xmlControl["Click"] = "OnRenderingClick(\"" + index + "\")";
                        xmlControl["DblClick"] = "device:edit";
                        if (index == selectedIndex)
                        {
                            xmlControl["Background"] = "#D0EBF6";
                        }
                        Controls.Add(uniqueID);
                        if (item != null)
                        {
                            xmlControl["ID"] = uniqueID;
                            xmlControl["Icon"] = item.Appearance.Icon;
                            xmlControl["Header"] = item.GetUIDisplayName();
                            xmlControl["Placeholder"] = WebUtil.SafeEncode(item2.Placeholder);
                        }
                        else
                        {
                            xmlControl["ID"] = uniqueID;
                            xmlControl["Icon"] = "Applications/24x24/forbidden.png";
                            xmlControl["Header"] = "Unknown rendering";
                            xmlControl["Placeholder"] = string.Empty;
                        }
                        if (item2.Rules != null && !item2.Rules.IsEmpty)
                        {
                            int num = item2.Rules.Elements("rule").Count();
                            if (num > 1)
                            {
                                HtmlGenericControl htmlGenericControl2 = new HtmlGenericControl("span");
                                if (num > 9)
                                {
                                    htmlGenericControl2.Attributes["class"] = "scConditionContainer scLongConditionContainer";
                                }
                                else
                                {
                                    htmlGenericControl2.Attributes["class"] = "scConditionContainer";
                                }
                                htmlGenericControl2.InnerText = num.ToString();
                                htmlGenericControl.Controls.Add(htmlGenericControl2);
                            }
                        }
                        RenderDeviceEditorRenderingPipeline.Run(item2, xmlControl, htmlGenericControl);
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the state of the commands.
        /// </summary>
        private void UpdateRenderingsCommandsState()
        {
            if (SelectedIndex < 0)
            {
                ChangeButtonsState(disable: true);
                return;
            }
            ArrayList renderings = GetLayoutDefinition().GetDevice(DeviceID).Renderings;
            if (renderings == null)
            {
                ChangeButtonsState(disable: true);
                return;
            }
            RenderingDefinition renderingDefinition = renderings[SelectedIndex] as RenderingDefinition;
            if (renderingDefinition == null)
            {
                ChangeButtonsState(disable: true);
                return;
            }
            ChangeButtonsState(disable: false);
            Personalize.Disabled = !string.IsNullOrEmpty(renderingDefinition.MultiVariateTest);
            Test.Disabled = HasRenderingRules(renderingDefinition);
        }

        private void UpdatePlaceholdersCommandsState()
        {
            phEdit.Disabled = string.IsNullOrEmpty(UniqueId);
            phRemove.Disabled = string.IsNullOrEmpty(UniqueId);
        }

        /// <summary>
        /// Changes the disable of the buttons.
        /// </summary>
        /// <param name="disable">if set to <c>true</c> buttons are disabled.</param>
        private void ChangeButtonsState(bool disable)
        {
            Personalize.Disabled = disable;
            btnEdit.Disabled = disable;
            btnChange.Disabled = disable;
            btnRemove.Disabled = disable;
            MoveUp.Disabled = disable;
            MoveDown.Disabled = disable;
            Test.Disabled = disable;
        }

        /// <summary>
        /// Determines whether [has rendering rules] [the specified definition].
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <returns><c>true</c> if the definition has a defined rule with action; otherwise, <c>false</c>.</returns>
        private static bool HasRenderingRules(RenderingDefinition definition)
        {
            if (definition.Rules == null)
            {
                return false;
            }
            foreach (XElement item in from rule in new RulesDefinition(definition.Rules.ToString()).GetRules()
                                      where rule.Attribute("uid").Value != ItemIDs.Null.ToString()
                                      select rule)
            {
                XElement xElement = item.Descendants("actions").FirstOrDefault();
                if (xElement != null && xElement.Descendants().Any())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Models.Audio.PipeWire;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Audio.Elements;

/// <summary>
/// Audio nodes of PipeWire of the given media classes - e.g. outputs, or playback streams.
/// A plain ListView rather than a wrapping widget, so a ScrolledWindow can scroll it directly
/// and rows stay recycled.
/// </summary>
public sealed class PipeWireNodeList : IDisposable, IWithDisposableService<AudioService>
{
    public AudioService Service { get; private set; }

    private readonly Gio.ListStore model = Gio.ListStore.New(PipeWireNode.GetGType());

    // Media class of a node never changes, so the nodes are filtered once, when added
    private readonly HashSet<string> mediaClasses;

    public ListView View { get; }

    /// <summary>
    /// Raised before the view lays out a change of its rows (added, removed) -
    /// the view still has its old size, see AudioDirectionPage.HoldListHeight.
    /// </summary>
    public event Action? RowsChanging;

    /// <param name="mediaClasses">Nodes to show, like <see cref="PipeWireMediaTypes.AudioSink"/></param>
    public PipeWireNodeList(AudioService service, params string[] mediaClasses)
    {
        this.mediaClasses = [.. mediaClasses];

        // Sort keys of a node never change, so the sorter is never refreshed
        var sortedModel = SortListModel.New(model, CustomSorter.New(CompareNodes));

        // Only shows the nodes - nothing to select
        var selection = NoSelection.New(sortedModel);
        View = ListView.New(selection, CreateRowFactory());

        // Connected after the view's own handler, so it runs once the view has taken the change
        selection.OnItemsChanged += (_, _) => RowsChanging?.Invoke();

        AddService(service);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(AudioService service)
    {
        Service = service;

        foreach (var node in Service.Nodes.Items)
            AddNode(node);

        Service.Nodes.Added += AddNode;
        Service.Nodes.Removed += RemoveNode;
        Service.Nodes.Cleared += ClearNodes;
    }

    public void RemoveService()
    {
        Service.Nodes.Added -= AddNode;
        Service.Nodes.Removed -= RemoveNode;
        Service.Nodes.Cleared -= ClearNodes;
    }

    /// <summary>Stops following the service and its nodes - they outlive the list.</summary>
    public void Dispose()
    {
        RemoveService();
        ClearNodes();
    }

    #region Nodes model

    private void AddNode(PipeWireNode node)
    {
        if (node.MediaClass is not null && mediaClasses.Contains(node.MediaClass))
            model.Append(node);
    }

    private void RemoveNode(PipeWireNode node)
    {
        for (uint i = 0; i < model.GetNItems(); i++)
        {
            if ((PipeWireNode?)model.GetObject(i) == node)
            {
                model.Remove(i);
                return;
            }
        }
    }

    private void ClearNodes() => model.RemoveAll();

    #endregion

    #region Sorting

    /// <summary>Outputs, inputs, then application streams - each group by name.</summary>
    private static int CompareNodes(nint a, nint b)
    {
        var first = NodeOf(a);
        var second = NodeOf(b);

        var byClass = ClassOrder(first.MediaClass).CompareTo(ClassOrder(second.MediaClass));
        if (byClass != 0)
            return byClass;

        var byName = string.Compare(
            first.DisplayName,
            second.DisplayName,
            StringComparison.CurrentCultureIgnoreCase
        );
        if (byName != 0)
            return byName;

        // Two streams of the same application (Zen opens several) keep a stable order
        return first.GlobalId.CompareTo(second.GlobalId);
    }

    private static int ClassOrder(string? mediaClass) =>
        mediaClass switch
        {
            PipeWireMediaTypes.AudioSink => 0,
            PipeWireMediaTypes.AudioSource => 1,
            PipeWireMediaTypes.StreamOutputAudio => 2,
            PipeWireMediaTypes.StreamInputAudio => 3,
            _ => 4,
        };

    // The sorter gets raw GObject pointers - the model only holds nodes
    private static PipeWireNode NodeOf(nint handle) =>
        (PipeWireNode)GObject.Internal.InstanceWrapper.WrapHandle<PipeWireNode>(handle, false);

    #endregion

    private SignalListItemFactory CreateRowFactory()
    {
        var factory = SignalListItemFactory.New();

        factory.OnSetup += (_, args) =>
            ((ListItem)args.Object).SetChild(PipeWireNodeListItem.New());

        factory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;

            if (
                listItem.GetChild() is PipeWireNodeListItem row
                && listItem.GetItem() is PipeWireNode node
            )
                row.Bind(node, Service);
            else
                System.Console.WriteLine($"Cannot bind audio node list row {listItem}");
        };

        factory.OnUnbind += (_, args) =>
            (((ListItem)args.Object).GetChild() as PipeWireNodeListItem)?.Unbind();

        return factory;
    }
}

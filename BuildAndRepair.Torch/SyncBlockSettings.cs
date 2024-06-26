﻿using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace BuildAndRepair.Torch;

/// <summary>
///     The settings for Block
/// </summary>
[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class SyncBlockSettings
{
    [Flags]
    public enum Settings
    {
        AllowBuild = 0x00000001,
        ShowArea = 0x00000002,
        ScriptControlled = 0x00000004,
        UseIgnoreColor = 0x00000010,
        UseGrindColor = 0x00000020,
        GrindNearFirst = 0x00000100,
        GrindSmallestGridFirst = 0x00000200,
        ComponentCollectIfIdle = 0x00010000,
        PushIngotOreImmediately = 0x00020000,
        PushComponentImmediately = 0x00040000,
        PushItemsImmediately = 0x00080000
    }

    private Vector3 _AreaOffset;
    private Vector3 _AreaSize;

    private int? _AreaWidthBottom;

    private int? _AreaWidthFront;

    private int? _AreaWidthLeft;

    private int? _AreaWidthRear;

    private int? _AreaWidthRight;

    private int? _AreaWidthTop;
    private string _ComponentCollectPriority;

    private IMySlimBlock _CurrentPickedGrindingBlock;
    private IMySlimBlock _CurrentPickedWeldingBlock;
    private Settings _Flags;
    private Vector3 _GrindColor;
    private AutoGrindOptions _GrindJanitorOptions;
    private string _GrindPriority;
    private Vector3 _IgnoreColor;
    private TimeSpan _LastStored;
    private TimeSpan _LastTransmitted;
    private SearchModes _SearchMode;
    private float _SoundVolume;
    private AutoGrindRelation _UseGrindJanitorOn;
    private AutoWeldOptions _WeldOptions;
    private string _WeldPriority;
    private WorkModes _WorkMode;

    public SyncBlockSettings() : this(null)
    {
    }

    public SyncBlockSettings(NanobotBuildAndRepairSystemBlock system)
    {
        _WeldPriority = string.Empty;
        _GrindPriority = string.Empty;
        _ComponentCollectPriority = string.Empty;
        CheckLimits(system, true);

        Changed = 0;
        _LastStored = MyAPIGateway.Session.ElapsedPlayTime.Add(TimeSpan.FromSeconds(60));
        _LastTransmitted = MyAPIGateway.Session.ElapsedPlayTime;

        RecalcAreaBoundigBox();
    }

    [XmlIgnore]
    public uint Changed { get; private set; }

    [ProtoMember(5)] [XmlElement]
    public Settings Flags
    {
        get => _Flags;
        set
        {
            if (_Flags != value)
            {
                _Flags = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(20)] [XmlElement]
    public SearchModes SearchMode
    {
        get => _SearchMode;
        set
        {
            if (_SearchMode != value)
            {
                _SearchMode = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(25)] [XmlElement]
    public WorkModes WorkMode
    {
        get => _WorkMode;
        set
        {
            if (_WorkMode != value)
            {
                _WorkMode = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(31)] [XmlElement]
    public Vector3 IgnoreColor
    {
        get => _IgnoreColor;
        set
        {
            if (_IgnoreColor != value)
            {
                _IgnoreColor = value;
                IgnoreColorPacked = value.PackHSVToUint();
                Changed = 3u;
            }
        }
    }

    [XmlIgnore]
    public uint IgnoreColorPacked { get; private set; }

    [ProtoMember(36)] [XmlElement]
    public Vector3 GrindColor
    {
        get => _GrindColor;
        set
        {
            if (_GrindColor != value)
            {
                _GrindColor = value;
                GrindColorPacked = value.PackHSVToUint();
                Changed = 3u;
            }
        }
    }

    [XmlIgnore]
    public uint GrindColorPacked { get; private set; }

    [ProtoMember(39)] [XmlElement]
    public AutoGrindRelation UseGrindJanitorOn
    {
        get => _UseGrindJanitorOn;
        set
        {
            if (_UseGrindJanitorOn != value)
            {
                _UseGrindJanitorOn = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(40)] [XmlElement]
    public AutoGrindOptions GrindJanitorOptions
    {
        get => _GrindJanitorOptions;
        set
        {
            if (_GrindJanitorOptions != value)
            {
                _GrindJanitorOptions = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(49)] [XmlElement]
    public AutoWeldOptions WeldOptions
    {
        get => _WeldOptions;
        set
        {
            if (_WeldOptions != value)
            {
                _WeldOptions = value;
                Changed = 3u;
            }
        }
    }

    //+X = Right   -Y = Left
    //+Y = Up      -Y = Down
    //+Z = Forward -Z = Backward
    [ProtoMember(50)] [XmlElement]
    public Vector3 AreaOffset
    {
        get => _AreaOffset;
        set
        {
            if (_AreaOffset != value)
            {
                _AreaOffset = value;
                Changed = 3u;
                RecalcAreaBoundigBox();
            }
        }
    }

    [ProtoMember(51)] [XmlElement]
    public Vector3 AreaSize
    {
        get => _AreaSize;
        set
        {
            if (_AreaSize != value)
            {
                _AreaSize = value;
                Changed = 3u;
                RecalcAreaBoundigBox();
            }
        }
    }

    [XmlElement]
    public int? AreaWidthLeft
    {
        get => null;
        set
        {
            _AreaWidthLeft = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [XmlElement]
    public int? AreaWidthRight
    {
        get => null;
        set
        {
            _AreaWidthRight = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [XmlElement]
    public int? AreaHeightTop
    {
        get => null;
        set
        {
            _AreaWidthTop = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [XmlElement]
    public int? AreaHeightBottom
    {
        get => null;
        set
        {
            _AreaWidthBottom = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [XmlElement]
    public int? AreaDepthFront
    {
        get => null;
        set
        {
            _AreaWidthFront = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [XmlElement]
    public int? AreaDepthRear
    {
        get => null;
        set
        {
            _AreaWidthRear = value;
            if (value != null) RecalcOffsetAndSize();
        }
    }

    [ProtoMember(61)] [XmlElement]
    public string WeldPriority
    {
        get => _WeldPriority;
        set
        {
            if (_WeldPriority != value)
            {
                _WeldPriority = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(62)] [XmlElement]
    public string GrindPriority
    {
        get => _GrindPriority;
        set
        {
            if (_GrindPriority != value)
            {
                _GrindPriority = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(65)] [XmlElement]
    public string ComponentCollectPriority
    {
        get => _ComponentCollectPriority;
        set
        {
            if (_ComponentCollectPriority != value)
            {
                _ComponentCollectPriority = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(80)] [XmlElement]
    public float SoundVolume
    {
        get => _SoundVolume;
        set
        {
            if (_SoundVolume != value)
            {
                _SoundVolume = value;
                Changed = 3u;
            }
        }
    }

    [XmlIgnore]
    public IMySlimBlock CurrentPickedWeldingBlock
    {
        get => _CurrentPickedWeldingBlock;
        set
        {
            if (_CurrentPickedWeldingBlock != value)
            {
                _CurrentPickedWeldingBlock = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(100)] [XmlElement]
    public SyncEntityId CurrentPickedWeldingBlockSync
    {
        get => SyncEntityId.GetSyncId(_CurrentPickedWeldingBlock);
        set => CurrentPickedWeldingBlock = SyncEntityId.GetItemAsSlimBlock(value);
    }

    [XmlIgnore]
    public IMySlimBlock CurrentPickedGrindingBlock
    {
        get => _CurrentPickedGrindingBlock;
        set
        {
            if (_CurrentPickedGrindingBlock != value)
            {
                _CurrentPickedGrindingBlock = value;
                Changed = 3u;
            }
        }
    }

    [ProtoMember(105)] [XmlElement]
    public SyncEntityId CurrentPickedGrindingBlockSync
    {
        get => SyncEntityId.GetSyncId(_CurrentPickedGrindingBlock);
        set => CurrentPickedGrindingBlock = SyncEntityId.GetItemAsSlimBlock(value);
    }

    [XmlIgnore]
    public int MaximumRange { get; private set; }

    [XmlIgnore]
    public int MaximumOffset { get; private set; }

    [XmlIgnore]
    public float TransportSpeed { get; private set; }

    [XmlIgnore]
    public float MaximumRequiredElectricPowerStandby { get; private set; }

    [XmlIgnore]
    public float MaximumRequiredElectricPowerWelding { get; private set; }

    [XmlIgnore]
    public float MaximumRequiredElectricPowerGrinding { get; private set; }

    [XmlIgnore]
    public float MaximumRequiredElectricPowerTransport { get; private set; }

    //+X = Forward -X = Backward
    //+Y = Left    -Y = Right
    //+Z = Up      -Z = Down
    internal Vector3 CorrectedAreaOffset { get; private set; }

    internal BoundingBoxD CorrectedAreaBoundingBox { get; private set; }

    private void SetFlags(bool set, Settings setting)
    {
        if (set != ((_Flags & setting) != 0))
        {
            _Flags = _Flags & ~setting | (set ? setting : 0);
            Changed = 3u;
        }
    }

    public void TrySave(IMyEntity entity, Guid guid)
    {
        if ((Changed & 2u) == 0) return;
        if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastStored) < TimeSpan.FromSeconds(20)) return;
        Save(entity, guid);
    }

    public void Save(IMyEntity entity, Guid guid)
    {
        if (entity.Storage == null)
            entity.Storage = new MyModStorageComponent();

        var storage = entity.Storage;
        storage[guid] = GetAsXML();
        Changed = Changed & ~2u;
        _LastStored = MyAPIGateway.Session.ElapsedPlayTime;
    }

    public string GetAsXML()
    {
        return MyAPIGateway.Utilities.SerializeToXML(this);
    }

    public void ResetChanged()
    {
        Changed = Changed & ~2u;
    }

    public static SyncBlockSettings Load(NanobotBuildAndRepairSystemBlock system, Guid guid, NanobotBuildAndRepairSystemBlockPriorityHandling blockWeldPriority, NanobotBuildAndRepairSystemBlockPriorityHandling blockGrindPriority,
                                         NanobotBuildAndRepairSystemComponentPriorityHandling componentCollectPriority)
    {
        var storage = system.Entity.Storage;
        SyncBlockSettings settings = null;
        if (storage != null && storage.TryGetValue(guid, out var data))
            try
            {
                //Fix changed names
                data = data.Replace("GrindColorNearFirst", "GrindNearFirst");
                settings = MyAPIGateway.Utilities.SerializeFromXML<SyncBlockSettings>(data);
                if (settings != null)
                {
                    settings.RecalcAreaBoundigBox();
                    //Retrieve current settings or default if WeldPriority/GrindPriority/ComponentCollectPriority was empty
                    blockWeldPriority.SetEntries(settings.WeldPriority);
                    settings.WeldPriority = blockWeldPriority.GetEntries();

                    blockGrindPriority.SetEntries(settings.GrindPriority);
                    settings.GrindPriority = blockGrindPriority.GetEntries();

                    componentCollectPriority.SetEntries(settings.ComponentCollectPriority);
                    settings.ComponentCollectPriority = componentCollectPriority.GetEntries();

                    settings.Changed = 0;
                    settings._LastStored = MyAPIGateway.Session.ElapsedPlayTime.Add(TimeSpan.FromSeconds(60));
                    settings._LastTransmitted = MyAPIGateway.Session.ElapsedPlayTime;
                    return settings;
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Write("SyncBlockSettings: Exception: " + ex);
            }

        settings = new(system);
        blockWeldPriority.SetEntries(settings.WeldPriority);
        blockGrindPriority.SetEntries(settings.GrindPriority);
        componentCollectPriority.SetEntries(settings.ComponentCollectPriority);
        settings.Changed = 0;
        return settings;
    }

    public void AssignReceived(SyncBlockSettings newSettings, NanobotBuildAndRepairSystemBlockPriorityHandling weldPriority, NanobotBuildAndRepairSystemBlockPriorityHandling grindPriority,
                               NanobotBuildAndRepairSystemComponentPriorityHandling componentCollectPriority)
    {
        _Flags = newSettings._Flags;
        _IgnoreColor = newSettings.IgnoreColor;
        _GrindColor = newSettings.GrindColor;
        _UseGrindJanitorOn = newSettings.UseGrindJanitorOn;
        _GrindJanitorOptions = newSettings.GrindJanitorOptions;
        _WeldOptions = newSettings.WeldOptions;

        _AreaOffset = newSettings.AreaOffset;
        _AreaSize = newSettings.AreaSize;

        _WeldPriority = newSettings.WeldPriority;
        _GrindPriority = newSettings.GrindPriority;
        _ComponentCollectPriority = newSettings.ComponentCollectPriority;

        _SoundVolume = newSettings.SoundVolume;
        _SearchMode = newSettings.SearchMode;
        _WorkMode = newSettings.WorkMode;

        RecalcAreaBoundigBox();
        IgnoreColorPacked = _IgnoreColor.PackHSVToUint();
        GrindColorPacked = _GrindColor.PackHSVToUint();
        weldPriority.SetEntries(WeldPriority);
        grindPriority.SetEntries(GrindPriority);
        componentCollectPriority.SetEntries(ComponentCollectPriority);

        Changed = 2u;
    }

    public SyncBlockSettings GetTransmit()
    {
        _LastTransmitted = MyAPIGateway.Session.ElapsedPlayTime;
        Changed = Changed & ~1u;
        return this;
    }

    public bool IsTransmitNeeded()
    {
        return (Changed & 1u) != 0 && MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastTransmitted) >= TimeSpan.FromSeconds(2);
    }

    private void RecalcAreaBoundigBox()
    {
        var border = 0.25d;
        CorrectedAreaBoundingBox = new(new(-AreaSize.Z / 2 + border, -AreaSize.X / 2 + border, -AreaSize.Y / 2 + border), new(AreaSize.Z / 2 - border, AreaSize.X / 2 - border, AreaSize.Y / 2 - border));
        CorrectedAreaOffset = new(AreaOffset.Z, -AreaOffset.X, AreaOffset.Y);
    }

    private void RecalcOffsetAndSize()
    {
        if (_AreaWidthLeft != null && _AreaWidthRight != null)
        {
            AreaSize = new(_AreaWidthRight.Value + _AreaWidthLeft.Value, AreaSize.Y, AreaSize.Z);
            AreaOffset = new(AreaSize.X / 2 - _AreaWidthRight.Value, AreaOffset.Y, AreaOffset.Z);
        }

        if (_AreaWidthTop != null && _AreaWidthBottom != null)
        {
            AreaSize = new(AreaSize.X, _AreaWidthTop.Value + _AreaWidthBottom.Value, AreaSize.Z);
            AreaOffset = new(AreaOffset.X, AreaSize.Y / 2 - _AreaWidthBottom.Value, AreaOffset.Z);
        }

        if (_AreaWidthFront != null && _AreaWidthRear != null)
        {
            AreaSize = new(AreaSize.X, AreaSize.Y, _AreaWidthFront.Value + _AreaWidthRear.Value);
            AreaOffset = new(AreaOffset.X, AreaOffset.Y, AreaSize.Z / 2 - _AreaWidthRear.Value);
        }
    }

    public void CheckLimits(NanobotBuildAndRepairSystemBlock system, bool init)
    {
        var scale = system?.Welder != null ? system.Welder.BlockDefinition.SubtypeName.Contains("Large") ? 1f : 3f : 1f;

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.AreaOffsetFixed || init)
        {
            MaximumOffset = 0;
            AreaOffset = new(0, 0, 0);
        }
        else
        {
            MaximumOffset = (int)Math.Ceiling(NanobotBuildAndRepairSystemMod.Settings.MaximumOffset / scale);
            if (AreaOffset.X > MaximumOffset || init) AreaOffset = new(init ? 0 : (float)MaximumOffset, AreaOffset.Y, AreaOffset.Z);
            else if (AreaOffset.X < -MaximumOffset || init) AreaOffset = new(init ? 0 : (float)-MaximumOffset, AreaOffset.Y, AreaOffset.Z);

            if (AreaOffset.Y > MaximumOffset || init) AreaOffset = new(AreaOffset.X, init ? 0 : (float)MaximumOffset, AreaOffset.Z);
            else if (AreaOffset.Y < -MaximumOffset || init) AreaOffset = new(AreaOffset.X, init ? 0 : (float)-MaximumOffset, AreaOffset.Z);

            if (AreaOffset.Z > MaximumOffset || init) AreaOffset = new(AreaOffset.X, AreaOffset.Y, init ? 0 : (float)MaximumOffset);
            else if (AreaOffset.Z < -MaximumOffset || init) AreaOffset = new(AreaOffset.X, AreaOffset.Y, init ? 0 : (float)-MaximumOffset);
        }

        MaximumRange = (int)Math.Ceiling(NanobotBuildAndRepairSystemMod.Settings.Range * 2 / scale);
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.AreaSizeFixed || init)
            AreaSize = new(MaximumRange, MaximumRange, MaximumRange);
        else
        {
            if (AreaSize.X > MaximumRange || init) AreaSize = new(MaximumRange, AreaSize.Y, AreaSize.Z);
            if (AreaSize.Y > MaximumRange || init) AreaSize = new(AreaSize.X, MaximumRange, AreaSize.Z);
            if (AreaSize.Z > MaximumRange || init) AreaSize = new(AreaSize.X, AreaSize.Y, MaximumRange);
        }

        MaximumRequiredElectricPowerStandby = NanobotBuildAndRepairSystemMod.Settings.MaximumRequiredElectricPowerStandby / scale;
        MaximumRequiredElectricPowerTransport = NanobotBuildAndRepairSystemMod.Settings.MaximumRequiredElectricPowerTransport / scale;
        MaximumRequiredElectricPowerWelding = NanobotBuildAndRepairSystemMod.Settings.Welder.MaximumRequiredElectricPowerWelding / scale;
        MaximumRequiredElectricPowerGrinding = NanobotBuildAndRepairSystemMod.Settings.Welder.MaximumRequiredElectricPowerGrinding / scale;

        var maxMultiplier = Math.Max(NanobotBuildAndRepairSystemMod.Settings.Welder.WeldingMultiplier, NanobotBuildAndRepairSystemMod.Settings.Welder.GrindingMultiplier);
        TransportSpeed = maxMultiplier * NanobotBuildAndRepairSystemBlock.WELDER_TRANSPORTSPEED_METER_PER_SECOND_DEFAULT *
                         Math.Min(NanobotBuildAndRepairSystemMod.Settings.Range / NanobotBuildAndRepairSystemBlock.WELDER_RANGE_DEFAULT_IN_M, 4.0f);

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.AllowBuildFixed || init)
            Flags = Flags & ~Settings.AllowBuild | (NanobotBuildAndRepairSystemMod.Settings.Welder.AllowBuildDefault ? Settings.AllowBuild : 0);

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.UseIgnoreColorFixed || init)
        {
            Flags = Flags & ~Settings.UseIgnoreColor | (NanobotBuildAndRepairSystemMod.Settings.Welder.UseIgnoreColorDefault ? Settings.UseIgnoreColor : 0);
            if (NanobotBuildAndRepairSystemMod.Settings.Welder.IgnoreColorDefault != null && NanobotBuildAndRepairSystemMod.Settings.Welder.IgnoreColorDefault.Length >= 3)
                IgnoreColor = new Vector3D(NanobotBuildAndRepairSystemMod.Settings.Welder.IgnoreColorDefault[0] / 360f,
                                           (float)Math.Round(NanobotBuildAndRepairSystemMod.Settings.Welder.IgnoreColorDefault[1], 1, MidpointRounding.AwayFromZero) / 100f - NanobotBuildAndRepairSystemTerminal.SATURATION_DELTA,
                                           (float)Math.Round(NanobotBuildAndRepairSystemMod.Settings.Welder.IgnoreColorDefault[2], 1, MidpointRounding.AwayFromZero) / 100f - NanobotBuildAndRepairSystemTerminal.VALUE_DELTA +
                                           NanobotBuildAndRepairSystemTerminal.VALUE_COLORIZE_DELTA);
        }

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.UseGrindColorFixed || init)
        {
            Flags = Flags & ~Settings.UseGrindColor | (NanobotBuildAndRepairSystemMod.Settings.Welder.UseGrindColorDefault ? Settings.UseGrindColor : 0);
            if (NanobotBuildAndRepairSystemMod.Settings.Welder.GrindColorDefault != null && NanobotBuildAndRepairSystemMod.Settings.Welder.GrindColorDefault.Length >= 3)
                GrindColor = new Vector3D(NanobotBuildAndRepairSystemMod.Settings.Welder.GrindColorDefault[0] / 360f,
                                          (float)Math.Round(NanobotBuildAndRepairSystemMod.Settings.Welder.GrindColorDefault[1], 1, MidpointRounding.AwayFromZero) / 100f - NanobotBuildAndRepairSystemTerminal.SATURATION_DELTA,
                                          (float)Math.Round(NanobotBuildAndRepairSystemMod.Settings.Welder.GrindColorDefault[2], 1, MidpointRounding.AwayFromZero) / 100f - NanobotBuildAndRepairSystemTerminal.VALUE_DELTA +
                                          NanobotBuildAndRepairSystemTerminal.VALUE_COLORIZE_DELTA);
        }

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.UseGrindJanitorFixed || init)
        {
            UseGrindJanitorOn = NanobotBuildAndRepairSystemMod.Settings.Welder.UseGrindJanitorDefault;
            GrindJanitorOptions = NanobotBuildAndRepairSystemMod.Settings.Welder.GrindJanitorOptionsDefault;
        }

        UseGrindJanitorOn &= NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedGrindJanitorRelations;

        if (NanobotBuildAndRepairSystemMod.Settings.Welder.ShowAreaFixed || init) Flags = Flags & ~Settings.ShowArea;
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.PushIngotOreImmediatelyFixed || init)
            Flags = Flags & ~Settings.PushIngotOreImmediately | (NanobotBuildAndRepairSystemMod.Settings.Welder.PushIngotOreImmediatelyDefault ? Settings.PushIngotOreImmediately : 0);
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.PushComponentImmediatelyFixed || init)
            Flags = Flags & ~Settings.PushComponentImmediately | (NanobotBuildAndRepairSystemMod.Settings.Welder.PushComponentImmediatelyDefault ? Settings.PushComponentImmediately : 0);
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.PushItemsImmediatelyFixed || init)
            Flags = Flags & ~Settings.PushItemsImmediately | (NanobotBuildAndRepairSystemMod.Settings.Welder.PushItemsImmediatelyDefault ? Settings.PushItemsImmediately : 0);
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.CollectIfIdleFixed || init)
            Flags = Flags & ~Settings.ComponentCollectIfIdle | (NanobotBuildAndRepairSystemMod.Settings.Welder.CollectIfIdleDefault ? Settings.ComponentCollectIfIdle : 0);
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.SoundVolumeFixed || init) SoundVolume = NanobotBuildAndRepairSystemMod.Settings.Welder.SoundVolumeDefault;
        if (NanobotBuildAndRepairSystemMod.Settings.Welder.ScriptControllFixed || init) Flags = Flags & ~Settings.ScriptControlled;
        if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedSearchModes & SearchMode) == 0 || init)
        {
            if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedSearchModes & NanobotBuildAndRepairSystemMod.Settings.Welder.SearchModeDefault) != 0)
                SearchMode = NanobotBuildAndRepairSystemMod.Settings.Welder.SearchModeDefault;
            else
            {
                if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedSearchModes & SearchModes.Grids) != 0) SearchMode = SearchModes.Grids;
                else if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedSearchModes & SearchModes.BoundingBox) != 0) SearchMode = SearchModes.BoundingBox;
            }
        }

        if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedWorkModes & WorkMode) == 0 || init)
        {
            if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedWorkModes & NanobotBuildAndRepairSystemMod.Settings.Welder.WorkModeDefault) != 0)
                WorkMode = NanobotBuildAndRepairSystemMod.Settings.Welder.WorkModeDefault;
            else
            {
                if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedWorkModes & WorkModes.WeldBeforeGrind) != 0) WorkMode = WorkModes.WeldBeforeGrind;
                else if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedWorkModes & WorkModes.GrindBeforeWeld) != 0) WorkMode = WorkModes.GrindBeforeWeld;
                else if ((NanobotBuildAndRepairSystemMod.Settings.Welder.AllowedWorkModes & WorkModes.GrindIfWeldGetStuck) != 0) WorkMode = WorkModes.GrindIfWeldGetStuck;
            }
        }
    }
}
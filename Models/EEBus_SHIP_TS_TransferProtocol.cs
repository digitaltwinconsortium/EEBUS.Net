﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("connectionHello", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class ConnectionHelloType {
    
    private ConnectionHelloPhaseType phaseField;
    
    private uint waitingField;
    
    private bool waitingFieldSpecified;
    
    private bool prolongationRequestField;
    
    private bool prolongationRequestFieldSpecified;
    
    /// <remarks/>
    public ConnectionHelloPhaseType phase {
        get {
            return this.phaseField;
        }
        set {
            this.phaseField = value;
        }
    }
    
    /// <remarks/>
    public uint waiting {
        get {
            return this.waitingField;
        }
        set {
            this.waitingField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool waitingSpecified {
        get {
            return this.waitingFieldSpecified;
        }
        set {
            this.waitingFieldSpecified = value;
        }
    }
    
    /// <remarks/>
    public bool prolongationRequest {
        get {
            return this.prolongationRequestField;
        }
        set {
            this.prolongationRequestField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool prolongationRequestSpecified {
        get {
            return this.prolongationRequestFieldSpecified;
        }
        set {
            this.prolongationRequestFieldSpecified = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum ConnectionHelloPhaseType {
    
    /// <remarks/>
    pending,
    
    /// <remarks/>
    ready,
    
    /// <remarks/>
    aborted,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public partial class ExtensionType {
    
    private string extensionIdField;
    
    private byte[] binaryField;
    
    private string stringField;
    
    /// <remarks/>
    public string extensionId {
        get {
            return this.extensionIdField;
        }
        set {
            this.extensionIdField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(DataType="hexBinary")]
    public byte[] binary {
        get {
            return this.binaryField;
        }
        set {
            this.binaryField = value;
        }
    }
    
    /// <remarks/>
    public string @string {
        get {
            return this.stringField;
        }
        set {
            this.stringField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public partial class HeaderType {
    
    private string protocolIdField;
    
    /// <remarks/>
    public string protocolId {
        get {
            return this.protocolIdField;
        }
        set {
            this.protocolIdField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("messageProtocolHandshake", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class MessageProtocolHandshakeType {
    
    private ProtocolHandshakeTypeType handshakeTypeField;
    
    private MessageProtocolHandshakeTypeVersion versionField;
    
    private string[] formatsField;
    
    /// <remarks/>
    public ProtocolHandshakeTypeType handshakeType {
        get {
            return this.handshakeTypeField;
        }
        set {
            this.handshakeTypeField = value;
        }
    }
    
    /// <remarks/>
    public MessageProtocolHandshakeTypeVersion version {
        get {
            return this.versionField;
        }
        set {
            this.versionField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("format", IsNullable=false)]
    public string[] formats {
        get {
            return this.formatsField;
        }
        set {
            this.formatsField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum ProtocolHandshakeTypeType {
    
    /// <remarks/>
    announceMax,
    
    /// <remarks/>
    select,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://docs.eebus.org/ship/xsd/v1")]
public partial class MessageProtocolHandshakeTypeVersion {
    
    private ushort majorField;
    
    private ushort minorField;
    
    /// <remarks/>
    public ushort major {
        get {
            return this.majorField;
        }
        set {
            this.majorField = value;
        }
    }
    
    /// <remarks/>
    public ushort minor {
        get {
            return this.minorField;
        }
        set {
            this.minorField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("messageProtocolHandshakeError", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class MessageProtocolHandshakeErrorType {
    
    private byte errorField;
    
    /// <remarks/>
    public byte error {
        get {
            return this.errorField;
        }
        set {
            this.errorField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("connectionPinState", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class ConnectionPinStateType {
    
    private PinStateType pinStateField;
    
    private PinInputPermissionType inputPermissionField;
    
    private bool inputPermissionFieldSpecified;
    
    /// <remarks/>
    public PinStateType pinState {
        get {
            return this.pinStateField;
        }
        set {
            this.pinStateField = value;
        }
    }
    
    /// <remarks/>
    public PinInputPermissionType inputPermission {
        get {
            return this.inputPermissionField;
        }
        set {
            this.inputPermissionField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool inputPermissionSpecified {
        get {
            return this.inputPermissionFieldSpecified;
        }
        set {
            this.inputPermissionFieldSpecified = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum PinStateType {
    
    /// <remarks/>
    required,
    
    /// <remarks/>
    optional,
    
    /// <remarks/>
    pinOk,
    
    /// <remarks/>
    none,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum PinInputPermissionType {
    
    /// <remarks/>
    busy,
    
    /// <remarks/>
    ok,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("connectionPinInput", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class ConnectionPinInputType {
    
    private string pinField;
    
    /// <remarks/>
    public string pin {
        get {
            return this.pinField;
        }
        set {
            this.pinField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("connectionPinError", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class ConnectionPinErrorType {
    
    private byte errorField;
    
    /// <remarks/>
    public byte error {
        get {
            return this.errorField;
        }
        set {
            this.errorField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("data", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class DataType {
    
    private HeaderType headerField;
    
    private object payloadField;
    
    private ExtensionType extensionField;
    
    /// <remarks/>
    public HeaderType header {
        get {
            return this.headerField;
        }
        set {
            this.headerField = value;
        }
    }
    
    /// <remarks/>
    public object payload {
        get {
            return this.payloadField;
        }
        set {
            this.payloadField = value;
        }
    }
    
    /// <remarks/>
    public ExtensionType extension {
        get {
            return this.extensionField;
        }
        set {
            this.extensionField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("connectionClose", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class ConnectionCloseType {
    
    private ConnectionClosePhaseType phaseField;
    
    private uint maxTimeField;
    
    private bool maxTimeFieldSpecified;
    
    private ConnectionCloseReasonType reasonField;
    
    private bool reasonFieldSpecified;
    
    /// <remarks/>
    public ConnectionClosePhaseType phase {
        get {
            return this.phaseField;
        }
        set {
            this.phaseField = value;
        }
    }
    
    /// <remarks/>
    public uint maxTime {
        get {
            return this.maxTimeField;
        }
        set {
            this.maxTimeField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool maxTimeSpecified {
        get {
            return this.maxTimeFieldSpecified;
        }
        set {
            this.maxTimeFieldSpecified = value;
        }
    }
    
    /// <remarks/>
    public ConnectionCloseReasonType reason {
        get {
            return this.reasonField;
        }
        set {
            this.reasonField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool reasonSpecified {
        get {
            return this.reasonFieldSpecified;
        }
        set {
            this.reasonFieldSpecified = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum ConnectionClosePhaseType {
    
    /// <remarks/>
    announce,
    
    /// <remarks/>
    confirm,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
public enum ConnectionCloseReasonType {
    
    /// <remarks/>
    unspecific,
    
    /// <remarks/>
    removedConnection,
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("accessMethodsRequest", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class AccessMethodsRequestType {
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://docs.eebus.org/ship/xsd/v1")]
[System.Xml.Serialization.XmlRootAttribute("accessMethods", Namespace="http://docs.eebus.org/ship/xsd/v1", IsNullable=false)]
public partial class AccessMethodsType {
    
    private string idField;
    
    private AccessMethodsTypeDnsSd_mDns dnsSd_mDnsField;
    
    private AccessMethodsTypeDns dnsField;
    
    /// <remarks/>
    public string id {
        get {
            return this.idField;
        }
        set {
            this.idField = value;
        }
    }
    
    /// <remarks/>
    public AccessMethodsTypeDnsSd_mDns dnsSd_mDns {
        get {
            return this.dnsSd_mDnsField;
        }
        set {
            this.dnsSd_mDnsField = value;
        }
    }
    
    /// <remarks/>
    public AccessMethodsTypeDns dns {
        get {
            return this.dnsField;
        }
        set {
            this.dnsField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://docs.eebus.org/ship/xsd/v1")]
public partial class AccessMethodsTypeDnsSd_mDns {
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://docs.eebus.org/ship/xsd/v1")]
public partial class AccessMethodsTypeDns {
    
    private string uriField;
    
    /// <remarks/>
    public string uri {
        get {
            return this.uriField;
        }
        set {
            this.uriField = value;
        }
    }
}

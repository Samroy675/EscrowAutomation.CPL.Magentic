using System.Text.Json.Serialization;

namespace SPSOrderFunctionApp.Models;

// ── Inbound Service Bus message ──────────────────────────────────────────────

public class SpsOrderRequestMessage
{
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("sourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;

    [JsonPropertyName("sourceAccount")]
    public string SourceAccount { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = string.Empty;
}

// ── SPS Order Read API request ───────────────────────────────────────────────

public class SpsApiRequest
{
    [JsonPropertyName("DocumentVersion")]
    public string DocumentVersion { get; set; } = "2.0";

    [JsonPropertyName("Service")]
    public SpsService Service { get; set; } = new();

    [JsonPropertyName("Payload")]
    public SpsPayload Payload { get; set; } = new();
}

public class SpsService
{
    [JsonPropertyName("Request")]
    public SpsServiceRequest Request { get; set; } = new();

    [JsonPropertyName("Select")]
    public SpsServiceSelect Select { get; set; } = new();
}

public class SpsServiceRequest
{
    [JsonPropertyName("SourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;

    [JsonPropertyName("SourceAccount")]
    public string SourceAccount { get; set; } = string.Empty;
}

public class SpsServiceSelect
{
    [JsonPropertyName("Profile")]
    public string Profile { get; set; } = string.Empty;
}

public class SpsPayload
{
    [JsonPropertyName("Select")]
    public SpsPayloadSelect Select { get; set; } = new();
}

public class SpsPayloadSelect
{
    [JsonPropertyName("Order")]
    public SpsOrderSelect Order { get; set; } = new();
}

public class SpsOrderSelect
{
    [JsonPropertyName("OrderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("UseDefaults")]
    public bool UseDefaults { get; set; } = false;

    [JsonPropertyName("OrderData")]
    public SpsOrderDataSelect OrderData { get; set; } = new();
}

public class SpsOrderDataSelect
{
    [JsonPropertyName("RequestedTasks")]
    public List<SpsRequestedTaskSelect> RequestedTasks { get; set; } = [new()];

    [JsonPropertyName("Properties")]
    public SpsPropertiesSelect Properties { get; set; } = new();

    [JsonPropertyName("Loans")]
    public List<SpsLoanSelect> Loans { get; set; } = [new()];

    [JsonPropertyName("Buyers")]
    public List<SpsPartySelect> Buyers { get; set; } = [new()];

    [JsonPropertyName("Lenders")]
    public List<SpsLenderSelect> Lenders { get; set; } = [new()];

    [JsonPropertyName("SettlementAgents")]
    public List<SpsSettlementAgentSelect> SettlementAgents { get; set; } = [new()];

    [JsonPropertyName("Notes")]
    public List<SpsNoteSelect> Notes { get; set; } = [new()];
}

public class SpsRequestedTaskSelect
{
    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("AssignedTo")]
    public List<SpsAssignedToSelect> AssignedTo { get; set; } = [new()];
}

public class SpsAssignedToSelect
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}

public class SpsPropertiesSelect
{
    [JsonPropertyName("Address")]
    public SpsAddressSelect Address { get; set; } = new();
}

public class SpsAddressSelect
{
    [JsonPropertyName("Address1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("City")]
    public string City { get; set; } = string.Empty;
}

public class SpsLoanSelect
{
    [JsonPropertyName("Number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("Funding")]
    public SpsLoanFundingSelect Funding { get; set; } = new();
}

public class SpsLoanFundingSelect
{
    [JsonPropertyName("LoanAmount")]
    public string LoanAmount { get; set; } = string.Empty;
}

public class SpsPartySelect
{
    [JsonPropertyName("BuyerSellerType")]
    public string BuyerSellerType { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsFullAddressSelect Address { get; set; } = new();

    [JsonPropertyName("People")]
    public List<SpsPersonSelect> People { get; set; } = [new()];
}

// Lenders do not have BuyerSellerType per the API contract
public class SpsLenderSelect
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsFullAddressSelect Address { get; set; } = new();

    [JsonPropertyName("People")]
    public List<SpsPersonSelect> People { get; set; } = [new()];
}

public class SpsFullAddressSelect
{
    [JsonPropertyName("Address1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("Address2")]
    public string Address2 { get; set; } = string.Empty;

    [JsonPropertyName("City")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("State")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("Zip")]
    public string Zip { get; set; } = string.Empty;
}

public class SpsPersonSelect
{
    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("Cell")]
    public string Cell { get; set; } = string.Empty;

    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("Fax")]
    public string Fax { get; set; } = string.Empty;

    [JsonPropertyName("Phone")]
    public string Phone { get; set; } = string.Empty;
}

public class SpsSettlementAgentSelect
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsFullAddressSelect Address { get; set; } = new();
}

public class SpsNoteSelect
{
    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("Categories")]
    public string Categories { get; set; } = string.Empty;

    [JsonPropertyName("CreatedOn")]
    public string CreatedOn { get; set; } = string.Empty;
}

// ── SPS Order Read API response ──────────────────────────────────────────────

public class SpsApiResponse
{
    [JsonPropertyName("Result")]
    public SpsResult? Result { get; set; }
}

public class SpsResult
{
    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Messages")]
    public List<string> Messages { get; set; } = [];

    [JsonPropertyName("SelectOrderNumber")]
    public string SelectOrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("SelectOrderGuid")]
    public string SelectOrderGuid { get; set; } = string.Empty;

    [JsonPropertyName("TransactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("Select")]
    public SpsResultSelect? Select { get; set; }
}

public class SpsResultSelect
{
    [JsonPropertyName("Order")]
    public SpsResultOrder? Order { get; set; }
}

public class SpsResultOrder
{
    [JsonPropertyName("OrderData")]
    public SpsResultOrderData? OrderData { get; set; }
}

public class SpsResultOrderData
{
    [JsonPropertyName("RequestedTasks")]
    public List<SpsRequestedTask>? RequestedTasks { get; set; }

    [JsonPropertyName("Properties")]
    public List<SpsProperty>? Properties { get; set; }

    [JsonPropertyName("Loans")]
    public List<SpsLoan>? Loans { get; set; }

    [JsonPropertyName("Buyers")]
    public List<SpsParty>? Buyers { get; set; }

    [JsonPropertyName("Lenders")]
    public List<SpsParty>? Lenders { get; set; }

    [JsonPropertyName("SettlementAgents")]
    public List<SpsSettlementAgent>? SettlementAgents { get; set; }

    [JsonPropertyName("Notes")]
    public List<SpsNote>? Notes { get; set; }
}

public class SpsRequestedTask
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("AssignedTo")]
    public List<SpsAssignedTo>? AssignedTo { get; set; }
}

public class SpsAssignedTo
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}

public class SpsProperty
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsAddressSelect? Address { get; set; }
}

public class SpsLoan
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("Number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("Funding")]
    public SpsLoanFunding? Funding { get; set; }
}

public class SpsLoanFunding
{
    [JsonPropertyName("LoanAmount")]
    public decimal LoanAmount { get; set; }
}

public class SpsParty
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("BuyerSellerType")]
    public string BuyerSellerType { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsFullAddress? Address { get; set; }

    [JsonPropertyName("People")]
    public List<SpsPerson>? People { get; set; }
}

public class SpsFullAddress
{
    [JsonPropertyName("Address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("Address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("City")]
    public string? City { get; set; }

    [JsonPropertyName("State")]
    public string? State { get; set; }

    [JsonPropertyName("Zip")]
    public string? Zip { get; set; }
}

public class SpsPerson
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("Cell")]
    public string? Cell { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("Fax")]
    public string? Fax { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }
}

public class SpsSettlementAgent
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public SpsFullAddress? Address { get; set; }
}

public class SpsNote
{
    [JsonPropertyName("Guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("Categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("CreatedOn")]
    public string CreatedOn { get; set; } = string.Empty;
}

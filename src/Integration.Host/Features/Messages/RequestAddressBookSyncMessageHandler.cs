using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Integration.Host.Features.FileOrchestrator;
using MetalHeaven.Agent.Shared.External.Interfaces;
using MetalHeaven.Agent.Shared.External.Messages;
using Versioned.ExternalDataContracts.Contracts.AddressBook;

namespace Integration.Host.Features.Messages;

public class RequestAddressBookSyncMessageHandler : IAgentRequestHandler<RequestAddressBookSyncMessage>
{
    public Task<IAgentMessage> Handle(AgentRequest<RequestAddressBookSyncMessage> request, CancellationToken cancellationToken)
    {
        var msg = request.Message;
        // implement business logic here

        // create addressBookSyncRequestResponse message
        // this is a sample response, you should replace it with actual data from your business logic
        #region Example Response
        var result = new RequestAddressBookSyncMessageResponse
        {
            Relations =
            [
                new AgentRelationImportRequest
                {
                    Id = 1,
                    Code = "Debtor Code",
                    CompanyName = "Quotation Factory B.V.",
                    Email = "info@quotationfactory.com",
                    Phone = "+31(0)850047332",
                    Website = "https://www.quotationfactory.com",
                    PostalStreet = "Aalsterweg",
                    PostalHouseNumber = "262",
                    //PostalHouseNumberAddition = "",
                    PostalCity = "Eindhoven",
                    PostalZipCode = "5644RK",
                    PostalStateOrProvince = "Noord-Brabant",
                    PostalCountryCode = "NL",
                    PostalCountryName = "Netherlands",
                    LanguageCode = "",
                    SegmentName = "A",
                    Tags = [],
                    VatNumber = "",
                    // This is the VAT rate in percentage from 0 to 100
                    VatRatio = 21.0,
                    //[Optional]
                    // CoCNumber = "",
                    // CoCCountryCode = "NL",
                    // CoCCountryName = "Netherlands"
                    // CurrencyCode = "EUR", // used to convert the sales prices in the quotation to the correct currency
                    // RawMaterialIsProvided = false, // to define that this customer provides the raw material
                    PaymentTermsCode =
                        "60D", // to define the payment terms code for this customer (aligned with the ERP system)
                    DeliveryTermsCode =
                        "EXW", // to define the delivery terms code for this customer (aligned with the ERP system)

                }
            ],
            EventLogs = new List<EventLog>
            {
                new() { DateTime = DateTime.UtcNow, Level = EventLogLevel.Information, Message = "This is some random information" }
            }
        };
        #endregion

        throw new NotImplementedException();
        return Task.FromResult<IAgentMessage>(result);
    }
}



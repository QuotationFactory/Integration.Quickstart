# Guidelines Quotation Factory \<\> ERP Connectivity

The purpose of this document is to outline the most common connectivity
principles in case Quotation Factory is or should be connected with an ERP
system. The guidelines are defined from various viewpoints such as
**separation of responsibilities**, **information interchange**,
**connectivity**, etc.

------------------------------------------------------------------------

# Separation of Responsibilities

ERP systems in general have functionality for sales processes (Request
for Quotation, Quotation, Order Confirmation, Order). In addition, ERP
systems have CRM functionality to manage a customers database and
categorize customers in various segments.

Quotation Factory is a specialized sales platform that fully digitalizes the
sales process and heavily applies Artificial Intelligence to minimize
cumbersome manual work. As such, Quotation Factory replaces (part of) the sales
process as implemented in ERP systems.

This results in the following separation of responsibilities between
Quotation Factory and the ERP system:

  -------------------------------------------------------------------------
  Business Process    Quotation Factory         ERP               CAM
  ------------------- ----------------- ----------------- -----------------
  Receive Request for                                     
  Quotation                                               

  Determine Geometric                                     
  Cost Drivers                                            

  Estimate production                                     
  times                                                   

  Estimate material                                       
  needs                                                   

  Determine margins &                                     
  surcharges                                              

  Determine           Orchestrate                         
  manufacturability                                       

  Determine possible                                      
  delivery date(s)                                        

  Create Quotation                      Sync              

  Send Quotation      Or                Or                

  Adjust Quotation                                        

  Receive Order                                           
  Confirmation                                            

  Create/Cancel Order                   Sync              

  Create BoM & BoL                      Sync              

  Program machine(s)  Orchestrate                         

  Manage customer     Sync                                
  information                                             

  Manage (raw)        Sync                                
  materials and                                           
  prices                                                  

  Manage resources &  Sync                                
  rates                                                   

  Manage margins &    Sync                                
  surcharges                                              

  Purchase (raw)                                          
  materials                                               

  Production planning                                     

  Production progress Sync                                
  monitoring                                              

  Measure Sales KPIs                                      

  Pre- and            Optimize          Optimize resource 
  Post-Production     estimation        rates             
  Analysis            accuracy                            
  -------------------------------------------------------------------------

------------------------------------------------------------------------

# Information Interchange

The following table shows the suggested/preferred interchange of
information between Quotation Factory and ERP as well as the direction of the
information flow.

  -----------------------------------------------------------------------
  Information             From                    To
  ----------------------- ----------------------- -----------------------
  Bill of Materials       Quotation Factory               ERP
  (hierarchy)                                     

  Bill of Labor and       Quotation Factory               ERP
  routing                                         

  CAD Files               Quotation Factory               ERP / CAM

  Estimated material      Quotation Factory               ERP
  needs                                           

  Estimated production    Quotation Factory               ERP
  times                                           

  Calculated cost prices  Quotation Factory               ERP

  Calculated sales prices Quotation Factory               ERP

  Thumbnails (3D & 2D)    Quotation Factory               ERP

  Customer information    ERP / CRM               Quotation Factory

  Customer segmentation,  ERP / CRM               Quotation Factory
  margins & surcharges                            

  Articles & Materials    ERP                     Quotation Factory
  incl. prices                                    

  Resources & rates       ERP                     Quotation Factory

  Project status/progress ERP                     Quotation Factory

  Post-production         ERP                     Quotation Factory
  information (cycle                              
  times, material usage)                          

  Manufacturability       CAM                     Quotation Factory
  information                                     
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# Connectivity

The design philosophy behind Quotation Factory is that the platform should be
**open**, **extensible**, and **integrable**. Therefore the platform
supports several technologies for interoperability.

  ---------------------------------------------------------------------------------------------------
  Technology              Information Format                                  Applicability
  ----------------------- --------------------------------------------------- -----------------------
  Webhooks                Quotation Factory proprietary JSON format`<br>`{=html}All   Integrate with 3rd
                          CAM files`<br>`{=html}All UBL 2.1 documents         party tools using cloud
                                                                              services such as Zapier
                                                                              and IFTTT. This is
                                                                              **not fault tolerant**.

  Rh24 Agent              Quotation Factory proprietary JSON format`<br>`{=html}All   Integrate directly with
                          CAM files`<br>`{=html}All UBL 2.1 documents         on-premise systems such
                                                                              as ERP, MES and CAM.
                                                                              **Real-time and fault
                                                                              tolerant
                                                                              communication.**

  SCSN                    UBL 2.1`<br>`{=html}Request for                     Integrate indirectly
                          Quotation`<br>`{=html}Quotation`<br>`{=html}Order   and transparently with
                          Confirmation`<br>`{=html}Order                      registered SCSN
                                                                              parties. **Not
                                                                              real-time. Fault
                                                                              tolerant.**

  Basic Web API (REST)    All above mentioned documents                       For simple custom
                                                                              integrations

  Full Web API (REST)     All above mentioned documents as well as all        For custom applications
                          Quotation Factory functionalities                           using Quotation Factory
                                                                              services
  ---------------------------------------------------------------------------------------------------

------------------------------------------------------------------------

# Business Events

Quotation Factory is an **event-driven platform** and can notify the outside
world of specific events. These events are the basis for both the
**Webhook-based integration** and the **Quotation Factory Agent-based
integration**.

Currently Quotation Factory supports the following events:

  ------------------------------------------------------------------------------------------------------------------------------
  Event Type                          When Triggered
  ----------------------------------- ------------------------------------------------------------------------------------------
  Document Created                    CAD file created`<br>`{=html}CAD file converted`<br>`{=html}UBL RfQ
                                      created`<br>`{=html}UBL Quotation created`<br>`{=html}UBL Order created

  Project Status Changed              When project has
                                      status:`<br>`{=html}defined`<br>`{=html}quoted`<br>`{=html}ordered`<br>`{=html}cancelled
  ------------------------------------------------------------------------------------------------------------------------------

The project status events follow the same order/sequence as indicated by
the Quotation Factory User Interface.

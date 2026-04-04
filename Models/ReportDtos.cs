namespace SmartPO.Models
{
    // ═══════════════════════════════════════════════
    //  INVOICE
    // ═══════════════════════════════════════════════
    public class InvoiceHeaderDto
    {
        public string InvoiceNumber   { get; set; } = "";
        public string InvoiceDate     { get; set; } = "";
        public string PoNumber        { get; set; } = "";
        public string PoDate          { get; set; } = "";
        public string ContactName     { get; set; } = "";
        public string ContactNo       { get; set; } = "";
        public string VehicleNo       { get; set; } = "";
        public string SimDcNo         { get; set; } = "";
        public string YourDcNo        { get; set; } = "";
        public string Remarks         { get; set; } = "";
        public string FromName        { get; set; } = "";
        public string FromAddress     { get; set; } = "";
        public string FromEmail       { get; set; } = "";
        public string FromPhone       { get; set; } = "";
        public string FromGst         { get; set; } = "";
        public string CustomerName     { get; set; } = "";
        public string BillingAddress   { get; set; } = "";  // AddressLine1 only
        public string CustomerState    { get; set; } = "";
        public string CustomerPincode  { get; set; } = "";
        public string ShippingAddress  { get; set; } = "";
        public string CustomerGst      { get; set; } = "";
        public string CustomerPhone    { get; set; } = "";
        public string CustomerDcDate   { get; set; } = "";
        public decimal SubTotal       { get; set; }
        public decimal CgstPercent    { get; set; }
        public decimal SgstPercent    { get; set; }
        public decimal IgstPercent    { get; set; }
        public decimal CgstAmount     { get; set; }
        public decimal SgstAmount     { get; set; }
        public decimal IgstAmount     { get; set; }
        public decimal FreightAmount  { get; set; }
        public decimal RoundOff       { get; set; }
        public decimal GrandTotal     { get; set; }
        public decimal TotalQty       { get; set; }
        public string  AmountInWords  { get; set; } = "";
        public byte[]  LogoBytes      { get; set; } = Array.Empty<byte>();
    }

    public class InvoiceItemDto
    {
        public int     SNo         { get; set; }
        public string  Description { get; set; } = "";
        public string  HsnCode     { get; set; } = "";
        public decimal UnitPrice   { get; set; }
        public decimal Quantity    { get; set; }
        public decimal LineAmount  { get; set; }
    }

    // ═══════════════════════════════════════════════
    //  PURCHASE ORDER
    // ═══════════════════════════════════════════════
    public class PoHeaderDto
    {
        public string  PoNumber      { get; set; } = "";
        public string  PoDate        { get; set; } = "";
        public string  FromName      { get; set; } = "";
        public string  FromAddress   { get; set; } = "";
        public string  FromEmail     { get; set; } = "";
        public string  FromPhone     { get; set; } = "";
        public string  FromGst       { get; set; } = "";
        public string  CustomerName  { get; set; } = "";
        public string  CustomerAddress { get; set; } = "";
        public string  CustomerGst   { get; set; } = "";
        public decimal CgstPercent   { get; set; }
        public decimal SgstPercent   { get; set; }
        public decimal IgstPercent   { get; set; }
        public decimal TaxAmount     { get; set; }
        public decimal RoundOff      { get; set; }
        public decimal GrandTotal    { get; set; }
        public string  AmountInWords { get; set; } = "";
    }

    public class PoItemDto
    {
        public int     SNo         { get; set; }
        public string  Description { get; set; } = "";
        public string  HsnCode     { get; set; } = "";
        public decimal UnitPrice   { get; set; }
        public decimal Quantity    { get; set; }
        public decimal LineTotal   { get; set; }
    }

    // ═══════════════════════════════════════════════
    //  QUOTATION
    // ═══════════════════════════════════════════════
    public class QuotationHeaderDto
    {
        public string  QuotationNo      { get; set; } = "";
        public string  QuotationDate    { get; set; } = "";
        public string  ValidUntil       { get; set; } = "";
        public string  FromName         { get; set; } = "";
        public string  FromAddress      { get; set; } = "";
        public string  FromEmail        { get; set; } = "";
        public string  FromPhone        { get; set; } = "";
        public string  FromGst          { get; set; } = "";
        public string  CustomerName     { get; set; } = "";
        public string  CustomerAddress  { get; set; } = "";
        public string  CustomerGst      { get; set; } = "";
        public string  CustomerPhone    { get; set; } = "";
        public decimal TotalAmount      { get; set; }
    }

    public class QuotationItemDto
    {
        public int     SNo         { get; set; }
        public string  Description { get; set; } = "";
        public decimal UnitPrice   { get; set; }
        public int     Quantity    { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // ═══════════════════════════════════════════════
    //  DELIVERY CHALLAN
    // ═══════════════════════════════════════════════
    public class DcHeaderDto
    {
        public string DcNumber       { get; set; } = "";
        public string DcDate         { get; set; } = "";
        public string VehicleNo      { get; set; } = "";
        public string FromName       { get; set; } = "";
        public string FromAddress    { get; set; } = "";
        public string FromEmail      { get; set; } = "";
        public string FromPhone      { get; set; } = "";
        public string FromGst        { get; set; } = "";
        public string CustomerName   { get; set; } = "";
        public string CustomerAddress{ get; set; } = "";
        public string CustomerGst    { get; set; } = "";
        public string TargetCompany  { get; set; } = "";
    }

    public class DcItemDto
    {
        public int    SNo         { get; set; }
        public string Description { get; set; } = "";
        public int    Quantity    { get; set; }
        public string Unit        { get; set; } = "";
        public string Remarks     { get; set; } = "";
    }
}

# C# Sample Code for the CyberSource SDK

This repository contains working code samples which demonstrate C#/.NET integration with the CyberSource REST APIs through the [CyberSource .NET SDK](https://github.com/CyberSource/cybersource-rest-client-dotnet).

## Using the Sample Code

The samples are all completely independent and self-contained. You can analyze them to get an understanding of how a particular method works, or you can use the snippets as a starting point for your own project.

### Requirements

* [CyberSource Account](https://developer.cybersource.com/api/developer-guides/dita-gettingstarted/registration.html)
* [CyberSource API Keys](https://developer.cybersource.com/api/developer-guides/dita-gettingstarted/registration/createCertSharedKey.html)

### Target Frameworks

The projects inside this solution demonstrate how you can target the following frameworks:

* .NET Framework 4.6.1
* .NET Framework 4.7.1
* .NET Framework 4.8.1

The samples are organized into categories and common usage examples.

### Running the Samples

* Clone this repository

    ```bash
    git clone https://github.com/CyberSource/cybersource-rest-samples-dotnet.git
    ```

* Open the solution `Cybersource.Rest.Samples.Legacy.sln` in Visual Studio and perform a clean build

* Choose the starting project from the list. Options are:
    * `Cybersource.Rest.Samples.Net461`,
    * `Cybersource.Rest.Samples.Net472`,
    * `Cybersource.Rest.Samples.Net481`

* Run the console app and select a sample to execute.

  The console app can be executed from the `Debug` menu in Visual Studio.

  Alternatively, the console app can also be executed from `command prompt`.
  
  For example, to run the project targetting .NET Framework 4.6.1, run the following command:

    ```bash
    Cybersource.Rest.Samples.Net461\bin\Debug\net461\Cybersource.Rest.Samples.Net461.exe
    ```

## Setting Your API Credentials

To set your API credentials for an API request, configure the following information in `Shared\Source\Configuration.cs` file:

* HTTP Signature

    ```csharp
      authenticationType  = "http_signature"
      merchantID          = <your_merchant_id>
      merchantKeyId       = <your_key_serial_number>
      merchantsecretKey   = <your_shared_secret>
      useMetaKey          = false
      enableClientCert    = false
    ```

* JWT

    ```csharp
      authenticationType  = "jwt"
      merchantID          = <your_merchant_id>
      keyAlias            = <your_merchant_id>
      keyPassword         = <password_for_your_p12_file>
      keyFileName         = <name_of_your_p12_file>
      keysDirectory       = <directory_where_your_p12_file_is_stored>
      useMetaKey          = false
      enableClientCert    = false
    ```

* MetaKey HTTP Signature

  ```csharp
    authenticationType  = "http_signature"
    merchantID          = <your_transacting_merchant_id>
    merchantKeyId       = <your_metakey_serial_number>
    merchantsecretKey   = <your_metakey_shared_secret>
    portfolioId         = <your_portfolio_id>
    useMetaKey          = true
    enableClientCert    = false
  ```

* MetaKey JWT

  ```csharp
    authenticationType  = "jwt"
    merchantID          = <your_transacting_merchant_id>
    keyAlias            = <your_portfolio_id>
    keyPassword         = <password_for_your_portfolio_p12_file>
    keyFileName         = <name_of_your_portfolio_p12_file>
    keysDirectory       = <directory_where_your_portfolio_p12_file_is_stored>
    useMetaKey          = true
    enableClientCert    = false
  ```

## Switching between the sandbox environment and the production environment

CyberSource maintains a complete sandbox environment for testing and development purposes. This sandbox environment is an exact duplicate of our production environment with the transaction authorization and settlement process simulated.

By default, this SDK is configured to communicate with the sandbox environment. To switch to the production environment, set the appropriate environment constant in `Shared\Source\Configuration.cs` file.

For example:

```csharp
    // For TESTING use
    _configurationDictionary.Add("runEnvironment", "apitest.cybersource.com");

    // For PRODUCTION use
    // _configurationDictionary.Add("runEnvironment", "api.cybersource.com");
```

The [API Reference Guide](https://developer.cybersource.com/api/reference/api-reference.html) provides examples of what information is needed for a particular request and how that information would be formatted. Using those examples, you can easily determine what methods would be necessary to include that information in a request using this SDK.

### Logging

[![Generic badge](https://img.shields.io/badge/LOGGING-NEW-GREEN.svg)](https://shields.io/)

The logging framework implemented in the SDK and used in this application makes use of NLog, and standardizes the logging so that it can be integrated with the logging in the client application.

More information about this new logging framework can be found in this file : [Logging.md](Logging.md)

## Disclaimer

Cybersource may allow Customer to access, use, and/or test a Cybersource product or service that may still be in development or has not been market-tested (“Beta Product”) solely for the purpose of evaluating the functionality or marketability of the Beta Product (a “Beta Evaluation”). Notwithstanding any language to the contrary, the following terms shall apply with respect to Customer’s participation in any Beta Evaluation (and the Beta Product(s)) accessed thereunder: The Parties will enter into a separate form agreement detailing the scope of the Beta Evaluation, requirements, pricing, the length of the beta evaluation period (“Beta Product Form”). Beta Products are not, and may not become, Transaction Services and have not yet been publicly released and are offered for the sole purpose of internal testing and non-commercial evaluation. Customer’s use of the Beta Product shall be solely for the purpose of conducting the Beta Evaluation. Customer accepts all risks arising out of the access and use of the Beta Products. Cybersource may, in its sole discretion, at any time, terminate or discontinue the Beta Evaluation. Customer acknowledges and agrees that any Beta Product may still be in development and that Beta Product is provided “AS IS” and may not perform at the level of a commercially available service, may not operate as expected and may be modified prior to release. CYBERSOURCE SHALL NOT BE RESPONSIBLE OR LIABLE UNDER ANY CONTRACT, TORT (INCLUDING NEGLIGENCE), OR OTHERWISE RELATING TO A BETA PRODUCT OR THE BETA EVALUATION (A) FOR LOSS OR INACCURACY OF DATA OR COST OF PROCUREMENT OF SUBSTITUTE GOODS, SERVICES OR TECHNOLOGY, (B) ANY CLAIM, LOSSES, DAMAGES, OR CAUSE OF ACTION ARISING IN CONNECTION WITH THE BETA PRODUCT; OR (C) FOR ANY INDIRECT, INCIDENTAL OR CONSEQUENTIAL DAMAGES INCLUDING, BUT NOT LIMITED TO, LOSS OF REVENUES AND LOSS OF PROFITS.


## License

This repository is distributed under a proprietary license.

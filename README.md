# .NET Sample Code for the CyberSource SDK

[![Build Status](https://app.travis-ci.com/CyberSource/cybersource-rest-samples-csharp.svg?branch=master)](https://app.travis-ci.com/CyberSource/cybersource-rest-samples-csharp)

This repository contains two solutions that demonstrate integration with the CyberSource REST APIs through our SDKs.

Each project in these solutions make use of the [Cybersource REST .NET SDK](https://github.com/Cybersource/cybersource-rest-client-dotnet).

## Legacy .NET

The application `Cybersource.Rest.Samples.Legacy.sln` contains three projects, each of which targets a different legacy framework:

* .NET Framework 4.6.1
* .NET Framework 4.7.1
* .NET Framework 4.8.1

For a more detailed explanation of how to use this application, please refer to [README_LEGACY.md](./README_LEGACY.md).

## Modern .NET

The application `Cybersource.Rest.Samples.Modern.sln` contains a single project which is capable of targetting each of the following:

* .NET 5.0
* .NET 6.0
* .NET 7.0
* .NET 8.0
* .NET 9.0
* .NET 10.0

For a more detailed explanation of how to use this application, please refer to [README_MODERN.md](./README_MODERN.md).

## Run Environments

```csharp
// For TESTING use
_configurationDictionary.Add("runEnvironment", "apitest.cybersource.com");

// For PRODUCTION use
// _configurationDictionary.Add("runEnvironment", "api.cybersource.com");
```

## Disclaimer

Cybersource may allow Customer to access, use, and/or test a Cybersource product or service that may still be in development or has not been market-tested (“Beta Product”) solely for the purpose of evaluating the functionality or marketability of the Beta Product (a “Beta Evaluation”). Notwithstanding any language to the contrary, the following terms shall apply with respect to Customer’s participation in any Beta Evaluation (and the Beta Product(s)) accessed thereunder: The Parties will enter into a separate form agreement detailing the scope of the Beta Evaluation, requirements, pricing, the length of the beta evaluation period (“Beta Product Form”). Beta Products are not, and may not become, Transaction Services and have not yet been publicly released and are offered for the sole purpose of internal testing and non-commercial evaluation. Customer’s use of the Beta Product shall be solely for the purpose of conducting the Beta Evaluation. Customer accepts all risks arising out of the access and use of the Beta Products. Cybersource may, in its sole discretion, at any time, terminate or discontinue the Beta Evaluation. Customer acknowledges and agrees that any Beta Product may still be in development and that Beta Product is provided “AS IS” and may not perform at the level of a commercially available service, may not operate as expected and may be modified prior to release. CYBERSOURCE SHALL NOT BE RESPONSIBLE OR LIABLE UNDER ANY CONTRACT, TORT (INCLUDING NEGLIGENCE), OR OTHERWISE RELATING TO A BETA PRODUCT OR THE BETA EVALUATION (A) FOR LOSS OR INACCURACY OF DATA OR COST OF PROCUREMENT OF SUBSTITUTE GOODS, SERVICES OR TECHNOLOGY, (B) ANY CLAIM, LOSSES, DAMAGES, OR CAUSE OF ACTION ARISING IN CONNECTION WITH THE BETA PRODUCT; OR (C) FOR ANY INDIRECT, INCIDENTAL OR CONSEQUENTIAL DAMAGES INCLUDING, BUT NOT LIMITED TO, LOSS OF REVENUES AND LOSS OF PROFITS.

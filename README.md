Disclaimer: Please note that we no longer support older versions of SDKs and Modules. We recommend that the latest versions are used.

# README

# Contents
- Introduction
- Prerequisites
- Using the Gateway SDK
- License

# Introduction
This C Sharp SDK provides an easy method to integrate with the payment gateway.
 - The Gateway.cs file contains the main body of the SDK.
 - The HomeController.cs file is intended as a minimal guide to demonstrate a complete 3DSv2 authentication process. It has been extracted from an ASP.Net website and can be run by placing back into a similar application.

# Prerequisites

- The SDK requires the following prerequisites to be met in order to function correctly:
    - .Net Framework v4.7.2 or later.

> Please note that we can only offer support for the SDK itself (Gateway.cs). While every effort has been made to ensure the sample code is complete and bug free, it is only a guide and cannot be used in a production environment.

# Using the Gateway SDK
Instantiate the Gateway object ensuring you pass in your Merchant ID and secret key.

```
var gateway = new Gateway("100856", "Circle4Take40Idea", "https://gateway.cardstream.com/direct/" )
```

This is a minimal object creation, but you can also override the default _direct_, _hosted_ and _merchant password_ fields, should you need to. The object also supports HTTP proxying if you require it. Take a look at Gateway.cs to see the full method signatures

Once your object has been created. You create your request array, for example:

```
            var reqFields = new Dictionary<string, string>{
              {"merchantID", "100856"},
              {"action", "SALE"},
              {"type", "1"},
              {"transactionUnique", "randomstring123" },
              {"countryCode", "826"},
              {"currencyCode", "826"},
              {"amount", "1001"},
              {"cardNumber", "4012001037141112"},
              {"cardExpiryMonth", "12"},
              {"cardExpiryYear", "20"},
              {"cardCVV", "083"},
              {"customerName", "Test Customer"},
              {"customerEmail","test@testcustomer.com"},
              {"customerAddress", "16 Test Street"},
              {"customerPostcode", "TE15 5ST"},
              {"orderRef", "Test purchase"},
              {"remoteAddress", remoteAddress},
              {"merchantCategoryCode", "5411"},
              {"threeDSVersion", "2"},
              {"threeDSRedirectURL", url + "?acs=1"},

```

> NB: This is a sample request. The gateway features many more options. Please see our integration guides for more details.

Then, depending on your integration method, you'd either call:

```
gateway.DirectRequest(reqFields)
```

OR

```
gateway.HostedRequest(reqFields)
```

And then handle the response received from the gateway.

License
----
MIT

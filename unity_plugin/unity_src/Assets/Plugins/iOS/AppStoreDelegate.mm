/*******************************************************************************
 * Copyright 2012-2014 One Platform Foundation
 *
 *       Licensed under the Apache License, Version 2.0 (the "License");
 *       you may not use this file except in compliance with the License.
 *       You may obtain a copy of the License at
 *
 *           http://www.apache.org/licenses/LICENSE-2.0
 *
 *       Unless required by applicable law or agreed to in writing, software
 *       distributed under the License is distributed on an "AS IS" BASIS,
 *       WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *       See the License for the specific language governing permissions and
 *       limitations under the License.
 ******************************************************************************/

#import "AppStoreDelegate.h"
#import <StoreKit/StoreKit.h>

/**
 * Helper method to create C string copy
 * By default mono string marshaler creates .Net string for returned UTF-8 C string
 * and calls free for returned value, thus returned strings should be allocated on heap
 * @param string original C string
 */
char* MakeStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

/**
 * It is used to send callbacks to the Unity event handler
 * @param objectName name of the target GameObject
 * @param methodName name of the handler method
 * @param param message string
 */
extern void UnitySendMessage(const char* objectName, const char* methodName, const char* param);

/**
 * Name of the event handler object in Unity
 */
const char* EventHandler = "OpenIABEventManager";

@implementation AppStoreDelegate

// Internal

/**
 * Collection of product identifiers
 */
NSSet* m_skus;

/**
 * Map of product listings
 * Information is requested from the store
 */
NSMutableArray* m_skuMap;

/**
 * Dictionary {sku: product}
 */
NSMutableDictionary* m_productMap;


- (void)storePurchase:(NSString*)transaction forSku:(NSString*)sku
{
    NSUserDefaults *standardUserDefaults = [NSUserDefaults standardUserDefaults];
    if (standardUserDefaults)
    {
        [standardUserDefaults setObject:transaction forKey:sku];
        [standardUserDefaults synchronize];
    }
    else
        NSLog(@"Couldn't access standardUserDefaults. Purchase wasn't stored.");
}


// Init

+ (AppStoreDelegate*)instance
{
    static AppStoreDelegate* instance = nil;
    if (!instance)
        instance = [[AppStoreDelegate alloc] init];

    return instance;
}

- (id)init
{
    self = [super init];
    [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
    return self;
}

- (void)dealloc
{
    [[SKPaymentQueue defaultQueue] removeTransactionObserver:self];
    m_skus = nil;
    m_skuMap = nil;
    m_productMap = nil;
}


// Setup

- (void)requestSKUs:(NSSet*)skus
{
    m_skus = skus;
    SKProductsRequest *request = [[SKProductsRequest alloc] initWithProductIdentifiers:skus];
    request.delegate = self;
    [request start];
}

// Setup handler

- (void)productsRequest:(SKProductsRequest*)request didReceiveResponse:(SKProductsResponse*)response
{
    m_skuMap = [[NSMutableArray alloc] init];
    m_productMap = [[NSMutableDictionary alloc] init];

    NSArray* skProducts = response.products;
    for (SKProduct * skProduct in skProducts)
    {
        // Format the price
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
        [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
        [numberFormatter setLocale:skProduct.priceLocale];
        NSString *formattedPrice = [numberFormatter stringFromNumber:skProduct.price];

        NSLocale *priceLocale = skProduct.priceLocale;
        NSString *currencyCode = [priceLocale objectForKey:NSLocaleCurrencyCode];
        NSNumber *productPrice = skProduct.price;

        NSString *itemType = @"inapp";
        NSString *subscriptionCycles = @"unavailable";; // enum converted to an integer

        float introPrice = 0;
        NSString *introFormattedPrice = @"";

        NSUInteger introPricePeriod = 0;
        NSString *introPriceCycles = @"unavailable"; // enum converted to an integer

        NSUInteger trialPricePeriod = 0;
        NSString *trialPriceCycles = @"unavailable";; // enum converted to an integer

        // Subscriptions are only available in iOS 11.2 and later
        if(@available(iOS 11.2, *)){
            // Check if this item is a subscription
            if(skProduct.subscriptionPeriod != nil){
                itemType = @"subs";

                switch(skProduct.subscriptionPeriod.unit)
                {
                    case SKProductPeriodUnitDay: subscriptionCycles = @"day"; break;
                    case SKProductPeriodUnitWeek: subscriptionCycles = @"week"; break;
                    case SKProductPeriodUnitMonth: subscriptionCycles = @"month"; break;
                    case SKProductPeriodUnitYear: subscriptionCycles = @"year"; break;
                }

                SKProductDiscount *introDiscount = skProduct.introductoryPrice;

                if(introDiscount != nil){
                    // This value must be converted to a float otherwise it throws a memory error..
                    introPrice = introDiscount.price.floatValue;

                    introFormattedPrice = [numberFormatter stringFromNumber:introDiscount.price];

                    introPricePeriod = introDiscount.numberOfPeriods;

                    switch(introDiscount.subscriptionPeriod.unit)
                    {
                        case SKProductPeriodUnitDay: introPriceCycles = @"day"; break;
                        case SKProductPeriodUnitWeek: introPriceCycles = @"week"; break;
                        case SKProductPeriodUnitMonth: introPriceCycles = @"month"; break;
                        case SKProductPeriodUnitYear: introPriceCycles = @"year"; break;
                    }
                }

                // Discounts are only available in iOS 12.2 and later
                if(@available(iOS 12.2, *)){
                    if(skProduct.discounts != nil && skProduct.discounts.count > 0){
                        SKProductDiscount *trialOffer = skProduct.discounts[0];

                        trialPricePeriod = trialOffer.numberOfPeriods;

                        switch(trialOffer.subscriptionPeriod.unit)
                        {
                            case SKProductPeriodUnitDay: trialPriceCycles = @"day"; break;
                            case SKProductPeriodUnitWeek: trialPriceCycles = @"week"; break;
                            case SKProductPeriodUnitMonth: trialPriceCycles = @"month"; break;
                            case SKProductPeriodUnitYear: trialPriceCycles = @"year"; break;
                        }
                    }
                }
            }
        }

        // Setup sku details
        NSDictionary* skuDetails = [NSDictionary dictionaryWithObjectsAndKeys:
                                    itemType, @"itemType",
                                    skProduct.productIdentifier, @"sku",
                                    itemType, @"type",
                                    formattedPrice, @"price",
                                    currencyCode, @"currencyCode",
                                    productPrice, @"priceValue",
                                    ([skProduct.localizedTitle length] == 0) ? @"" : skProduct.localizedTitle, @"title",
                                    ([skProduct.localizedDescription length] == 0) ? @"" : skProduct.localizedDescription, @"description",
                                    @"", @"json",
                                    introPrice, @"introductoryPriceValue",
                                    introFormattedPrice, @"introductoryPriceFormatted",
                                    introPricePeriod, @"introductoryPricePeriod",
                                    introPriceCycles, @"introductoryPriceCycles",
                                    trialPricePeriod, @"freeTrialPeriod",
                                    trialPriceCycles, @"freeTrialCycles",
                                    subscriptionCycles, @"subscriptionPeriod",
                                    nil];

        NSArray* entry = [NSArray arrayWithObjects:skProduct.productIdentifier, skuDetails, nil];
        [m_skuMap addObject:entry];
        [m_productMap setObject:skProduct forKey:skProduct.productIdentifier];
    }

    UnitySendMessage(EventHandler, "OnBillingSupported", MakeStringCopy(""));
}

- (void)request:(SKRequest*)request didFailWithError:(NSError*)error
{
    UnitySendMessage(EventHandler, "OnBillingNotSupported", MakeStringCopy([[error localizedDescription] UTF8String]));
}


// Transactions

- (void)startPurchase:(NSString*)sku
{
    SKProduct* product = m_productMap[sku];
    SKMutablePayment *payment = [SKMutablePayment paymentWithProduct:product];
    [[SKPaymentQueue defaultQueue] addPayment:payment];
}

- (void)queryInventory
{
    NSMutableDictionary* inventory = [[NSMutableDictionary alloc] init];
    NSMutableArray* purchaseMap = [[NSMutableArray alloc] init];
    NSUserDefaults* standardUserDefaults = [NSUserDefaults standardUserDefaults];
    if (!standardUserDefaults)
        NSLog(@"Couldn't access purchase storage. Purchase map won't be available.");
    else
        for (NSString* sku in m_skus)
            if ([[[standardUserDefaults dictionaryRepresentation] allKeys] containsObject:sku])
            {
                NSString* encodedPurchase =  [standardUserDefaults objectForKey:sku];
                NSError *e = nil;
                NSDictionary* storedPurchase = [NSJSONSerialization JSONObjectWithData:
                                                [encodedPurchase dataUsingEncoding:NSUTF8StringEncoding]
                                                                               options: NSJSONReadingMutableContainers error: &e];
                if (!storedPurchase) {
                    NSLog(@"Got an error while creating the JSON object: %@", e);
                    continue;
                }

                // TODO: Probably store all purchase information. Not only sku
                // Setup purchase
                NSDictionary* purchase = [NSDictionary dictionaryWithObjectsAndKeys:
                                          @"product", @"itemType",
                                          [storedPurchase objectForKey:@"orderId"], @"orderId",
                                          [storedPurchase objectForKey:@"receipt"], @"receipt",
                                          @"", @"packageName",
                                          sku, @"sku",
                                          [NSNumber numberWithLong:0], @"purchaseTime",
                                          // TODO: copy constants from Android if ever needed
                                          [NSNumber numberWithInt:0], @"purchaseState",
                                          @"", @"developerPayload",
                                          @"", @"token",
                                          @"", @"originalJson",
                                          @"", @"signature",
                                          @"", @"appstoreName",
                                          nil];

                NSArray* entry = [NSArray arrayWithObjects:sku, purchase, nil];
                [purchaseMap addObject:entry];
            }

    [inventory setObject:purchaseMap forKey:@"purchaseMap"];
    [inventory setObject:m_skuMap forKey:@"skuMap"];

    NSError* error = nil;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:inventory options:kNilOptions error:&error];
    NSString* message = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    UnitySendMessage(EventHandler, "OnQueryInventorySucceeded", MakeStringCopy([message UTF8String]));
}

- (void)restorePurchases
{
    [[SKPaymentQueue defaultQueue] restoreCompletedTransactions];
}


// Transactions handler

- (void)paymentQueue:(SKPaymentQueue *)queue updatedDownloads:(NSArray *)downloads
{
    // Required by store protocol
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    NSString* jsonTransaction;

    for (SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchasing:
            case SKPaymentTransactionStateDeferred:
                break;

            case SKPaymentTransactionStateFailed:
                if (transaction.error == nil)
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy("Transaction failed"));
                else if (transaction.error.code == SKErrorPaymentCancelled)
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy("Transaction cancelled"));
                else
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy([[transaction.error localizedDescription] UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;

            case SKPaymentTransactionStateRestored:
                jsonTransaction = [self convertTransactionToJson:transaction.originalTransaction storeToUserDefaults:true];
                if ([jsonTransaction  isEqual: @"error"])
                {
                    return;
                }

                UnitySendMessage(EventHandler, "OnPurchaseRestored", MakeStringCopy([jsonTransaction UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;

            case SKPaymentTransactionStatePurchased:
                jsonTransaction = [self convertTransactionToJson:transaction storeToUserDefaults:true];
                if ([jsonTransaction  isEqual: @"error"])
                {
                    return;
                }

                UnitySendMessage(EventHandler, "OnPurchaseSucceeded", MakeStringCopy([jsonTransaction UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
        }
    }
}

- (NSString*)convertTransactionToJson: (SKPaymentTransaction*) transaction storeToUserDefaults:(bool)store
{
    //NSURL *receiptURL = [[NSBundle mainBundle] appStoreReceiptURL];
    //NSData *receipt = [NSData dataWithContentsOfURL:receiptURL];
    //NSString *receiptBase64 = [receipt base64EncodedStringWithOptions:0];

    NSString *receiptBase64 = [transaction.transactionReceipt base64EncodedStringWithOptions:0];

    NSDictionary *requestContents = [NSDictionary dictionaryWithObjectsAndKeys:
                                     transaction.payment.productIdentifier, @"sku",
                                     transaction.transactionIdentifier, @"orderId",
                                     receiptBase64, @"receipt",
                                     nil];

    NSError *error;
    NSData *requestData = [NSJSONSerialization dataWithJSONObject:requestContents
                                                          options:0
                                                            error:&error];
    if (!requestData) {
        NSLog(@"Got an error while creating the JSON object: %@", error);
        return @"error";
    }

    NSString * jsonString = [[NSString alloc] initWithData:requestData encoding:NSUTF8StringEncoding];

    if (store){
        [self storePurchase:jsonString
                     forSku:transaction.payment.productIdentifier
         ];
    }

    return jsonString;
}

- (void)paymentQueue:(SKPaymentQueue*)queue restoreCompletedTransactionsFailedWithError:(NSError*)error
{
    UnitySendMessage(EventHandler, "OnRestoreFailed", MakeStringCopy([[error localizedDescription] UTF8String]));
}

- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue*)queue
{
    UnitySendMessage(EventHandler, "OnRestoreFinished", MakeStringCopy(""));
}

@end
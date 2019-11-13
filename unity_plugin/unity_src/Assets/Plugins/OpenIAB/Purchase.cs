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

using UnityEngine;

namespace OnePF
{
    /**
     * Represents an in-app billing purchase.
     */
    public class Purchase
    {
        /// <summary>
        /// ITEM_TYPE_INAPP or ITEM_TYPE_SUBS
        /// </summary>
        public string ItemType { get; private set; }
        /// <summary>
        /// A unique order identifier for the transaction. This corresponds to the Google Wallet Order ID.
        /// </summary>
        public string OrderId { get; private set; }
        /// <summary>
        /// The application package from which the purchase originated.
        /// </summary>
        public string PackageName { get; private set; }
        /// <summary>
        /// The item's product identifier. Every item has a product ID, which you must specify in the application's product list on the Google Play Developer Console.
        /// </summary>
        public string Sku { get; private set; }
        /// <summary>
        /// The time the product was purchased, in milliseconds since the epoch (Jan 1, 1970).
        /// </summary>
        public long PurchaseTime { get; private set; }
        /// <summary>
        /// The purchase state of the order. Possible values are 0 (purchased), 1 (canceled), or 2 (refunded).
        /// </summary>
        public int PurchaseState { get; private set; }
        /// <summary>
        /// A developer-specified string that contains supplemental information about an order. You can specify a value for this field when you make a getBuyIntent request.
        /// </summary>
        public string DeveloperPayload { get; private set; }
        /// <summary>
        /// A token that uniquely identifies a purchase for a given item and user pair.
        /// </summary>
        public string Token { get; private set; }
        /// <summary>
        /// JSON sent by the current store
        /// </summary>
        public string OriginalJson { get; private set; }
        /// <summary>
        /// Signature of the JSON string
        /// </summary>
        public string Signature { get; private set; }
        /// <summary>
        /// Current store name
        /// </summary>
        public string AppstoreName { get; private set; }
        /// <summary>
        /// Purchase Receipt of the order (iOS only)
        /// </summary>
        public string Receipt { get; private set; }

        private Purchase()
        {
        }

        /**
         * Create purchase from json string
         * @param jsonString data serialized to json
         */
        public Purchase(string jsonString)
        {
            var json = new JSON(jsonString);
            ItemType = json.ToString("itemType");
            OrderId = json.ToString("orderId");
            PackageName = json.ToString("packageName");
            Sku = json.ToString("sku");
            PurchaseTime = json.ToLong("purchaseTime");
            PurchaseState = json.ToInt("purchaseState");
            DeveloperPayload = json.ToString("developerPayload");
            Token = json.ToString("token");
            OriginalJson = json.ToString("originalJson");
            Signature = json.ToString("signature");
            AppstoreName = json.ToString("appstoreName");
            Receipt = json.ToString("receipt");

#if UNITY_IOS
            // Catch will be hit if the Sku is already a product id
            try
            {
                Sku = OpenIAB_iOS.StoreSku2Sku(Sku);
            }
            catch (System.Collections.Generic.KeyNotFoundException) { }

            JSON receiptJson = AppleArrayToJSON(Receipt);

            // Parse the iOS receipt token, converting it from Apple's own stupid format to normal JSON data along the way
            if (receiptJson != null)
            {
                string purchaseInfo = receiptJson.ToString("purchase-info");

                if (!string.IsNullOrEmpty(purchaseInfo))
                {
                    JSON purchaseInfoJson = AppleArrayToJSON(purchaseInfo);

                    if (purchaseInfoJson != null)
                    {
                        string transactionId = purchaseInfoJson.ToString("original-transaction-id");

                        if (!string.IsNullOrEmpty(transactionId))
                        {
                            Token = transactionId;
                        }
                        else
                        {
                            Debug.LogError("Failed to get transaction id");
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to get purchaseInfo JSON!");
                    }
                }
                else
                {
                    Debug.LogError("Failed to get purchase info");
                }
            }
            else
            {
                Debug.LogError("Failed to get receipt JSON!");
            }
#endif
        }

        // Instead of normal JSON apple has its own structure for receipt data..
        // This function converts apple's arrays to normal JSON data
        private JSON AppleArrayToJSON(string input)
        {
            string outputJson = string.Empty;

            if (!string.IsNullOrEmpty(input))
            {
                outputJson = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(input));
                outputJson = outputJson.Replace(" = ", ":").Replace(';', ',');

                int charRemovalPos = outputJson.LastIndexOf(',');

                if (charRemovalPos >= 0)
                    outputJson = outputJson.Remove(charRemovalPos, 1);
            }

            return new JSON(outputJson);
        }

#if UNITY_IOS
        public Purchase(JSON json)
        {

            if (json != null)
            {

                ItemType = json.ToString("itemType");
                OrderId = json.ToString("orderId");
                PackageName = json.ToString("packageName");
                Sku = json.ToString("sku");
                PurchaseTime = json.ToLong("purchaseTime");
                PurchaseState = json.ToInt("purchaseState");
                DeveloperPayload = json.ToString("developerPayload");
                Token = json.ToString("token");
                OriginalJson = json.ToString("originalJson");
                Signature = json.ToString("signature");
                AppstoreName = json.ToString("appstoreName");
                Receipt = json.ToString("receipt");

                // Catch will be hit if the Sku is already a product id
                try
                {
                    Sku = OpenIAB_iOS.StoreSku2Sku(Sku);
                } catch(System.Collections.Generic.KeyNotFoundException){}

                JSON receiptJson = AppleArrayToJSON(Receipt);

                if (receiptJson != null)
                {
                    string purchaseInfo = receiptJson.ToString("purchase-info");

                    if (!string.IsNullOrEmpty(purchaseInfo))
                    {
                        JSON purchaseInfoJson = AppleArrayToJSON(purchaseInfo);

                        if (purchaseInfoJson != null)
                        {
                            string transactionId = purchaseInfoJson.ToString("original-transaction-id");

                            if (!string.IsNullOrEmpty(transactionId))
                            {
                                Token = transactionId;
                            }
                            else
                            {
                                Debug.LogError("Failed to get transaction id");
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to get purchaseInfo JSON!");
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to get purchase info");
                    }
                }
                else
                {
                    Debug.LogError("Failed to get receipt JSON!");
                }

            }
            else
            {
                Debug.LogError("Null json!");
            }
        }
#endif

        /**
         * For debug purposes and editor mode
         * @param sku product ID
         */
        public static Purchase CreateFromSku(string sku)
        {
            return CreateFromSku(sku, "");
        }

        public static Purchase CreateFromSku(string sku, string developerPayload)
        {
            var p = new Purchase();
            p.Sku = sku;
            p.DeveloperPayload = developerPayload;
#if UNITY_IOS
            AddIOSHack(p);
#endif
            return p;
        }

        /**
         * ToString
         * @return original json
         */
        public override string ToString()
        {
            return "SKU:" + Sku + ";" + OriginalJson;
        }

#if UNITY_IOS
        private static void AddIOSHack(Purchase p)
        {
            if (string.IsNullOrEmpty(p.AppstoreName))
            {
                p.AppstoreName = "com.apple.appstore";
            }
            if (string.IsNullOrEmpty(p.ItemType))
            {
                p.ItemType = "InApp";
            }
            if (string.IsNullOrEmpty(p.OrderId))
            {
                p.OrderId = System.Guid.NewGuid().ToString();
            }
        }
#endif

        /**
         * Serilize to json
         * @return json string
         */
        public string Serialize()
        {
            var j = new JSON();
            j["itemType"] = ItemType;
            j["orderId"] = OrderId;
            j["packageName"] = PackageName;
            j["sku"] = Sku;
            j["purchaseTime"] = PurchaseTime;
            j["purchaseState"] = PurchaseState;
            j["developerPayload"] = DeveloperPayload;
            j["token"] = Token;
            j["originalJson"] = OriginalJson;
            j["signature"] = Signature;
            j["appstoreName"] = AppstoreName;
            j["receipt"] = Receipt;
            return j.serialized;
        }
    }
}
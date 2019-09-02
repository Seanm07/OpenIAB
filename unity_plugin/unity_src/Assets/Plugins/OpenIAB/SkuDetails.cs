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
     * Represents an in-app product's listing details.
     */
    public class SkuDetails
    {
        public string ItemType { get; private set; }
        public string Sku { get; private set; }
        public string Type { get; private set; }
        public string Price { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Json { get; private set; }
        public string CurrencyCode { get; private set; }
        public string PriceValue { get; private set; }

        // Subscription only variables
		public string SubscriptionPeriod { get; private set; }		// P1W, P1M, P3M, P6M, P1Y
		public string FreeTrialPeriod { get; private set; }			// P1D, P2D, P3D, P4D, P5D, P6D, P7D.. ETC
		public string IntroductoryPrice { get; private set; }		// £1.23, $1.23, €1.23.. ETC
		public string IntroductoryPriceValue { get; private set; } 	// 1.23.. ETC
		public string IntroductoryPricePeriod { get; private set; } // P1D, P2D, P1W, P2W, P1M, P2M.. ETC
		public string IntroductoryPriceCycles { get; private set; } // 1, 2, 3.. ETC

        // Used for Android
        public SkuDetails(string jsonString)
        {
        	/*
        		Explanation of what's happening:
        		The jsonString looks like this:

        		{
					"itemType": "subs",
					"sku": "premium_subscription",
					"type": "subs",
					"price": "£2.99",
					"title": "Premium Membership (Real Gangster - City Crime Simulator)",
					"description": "Become a premium member to earn awesome daily rewards and an ad-free \nexperience!",
					"json": "{
				  		\"productId\":\"premium_subscription\",
						\"type\":\"subs\",
						\"price\":\"£2.99\",
						\"price_amount_micros\":2990000,
						\"price_currency_code\":\"GBP\",
						\"subscriptionPeriod\":\"P1W\",
						\"freeTrialPeriod\":\"P3D\",
						\"introductoryPriceAmountMicros\":1190000,
						\"introductoryPricePeriod\":\"P1W\",
						\"introductoryPrice\":\"£1.19\",
						\"introductoryPriceCycles\":1,
						\"title\":\"Premium Membership (Real Gangster - City Crime Simulator)\",
						\"description\":\"Become a premium member to earn awesome daily rewards and an ad-free \\nexperience!\"
					}"
				}

				Note how only itemType, sku, type, price, title and description are directly available
				All the other values are within the "json" key
        	*/

            var json = new JSON(jsonString);

            ItemType = json.ToString("itemType");
            Sku = json.ToString("sku");
            Type = json.ToString("type");
            Price = json.ToString("price");
            Title = json.ToString("title");
            Description = json.ToString("description");

			Json = json.ToString("json");

            ParseFromJson();
        }

#if UNITY_IOS
        public SkuDetails(JSON json) {
            ItemType = json.ToString("itemType");
            Sku = json.ToString("sku");
            Type = json.ToString("type");
            Price = json.ToString("price");
            Title = json.ToString("title");
            Description = json.ToString("description");
            Json = json.ToString("json");
			CurrencyCode = json.ToString("currencyCode");
			PriceValue = json.ToString("priceValue");

            Sku = OpenIAB_iOS.StoreSku2Sku(Sku);

            ParseFromJsonIOS();

            Debug.Log("IOS DEBUGGING NEW PURCHASE!");
            Debug.Log(json.serialized);
        }

        private void ParseFromJsonIOS()
        {
            if (string.IsNullOrEmpty(Json)) return;

            var json = new JSON(Json);

            /*
             *"introductoryPriceValue\":0.00,
             * \"introductoryPriceFormatted\":\"\u00a30.00\",
             * \"introductoryPricePeriod\":\"1\",
             * \"introductoryPriceCycles\":\"week\",
             * \"subscriptionCycles\":\"7\",
             * \"subscriptionPeriod\":\"day\"
             */

            SubscriptionPeriod = ConvertToISO8601(json.ToString("subscriptionCycles"), json.ToString("subscriptionPeriod"));

            IntroductoryPriceValue = json.ToString("introductoryPriceValue");
            IntroductoryPrice = json.ToString("introductoryPriceFormatted");
            IntroductoryPricePeriod = ConvertToISO8601("1", json.ToString("introductoryPricePeriod"));
            IntroductoryPriceCycles = json.ToString("introductoryPriceCycles");
        }

        private string ConvertToISO8601(string cyclesInput, string periodInput)
        {
            string output = "P" + cyclesInput;

            switch(periodInput)
            {
                case "day": output += "d"; break;
                case "week": output += "w"; break;
                case "month": output += "m"; break;
                case "year": output += "y"; break;
            }

            return output;
        }
#endif

#if UNITY_WP8
        public SkuDetails(OnePF.WP8.ProductListing listing)
        {
            Sku = OpenIAB_WP8.GetSku(listing.ProductId);
            Title = listing.Name;
            Description = listing.Description;
            Price = listing.FormattedPrice;
        }
#endif

        private void ParseFromJson()
        {
            if (string.IsNullOrEmpty(Json)) return;

            var json = new JSON(Json);

			PriceValue = (json.ToFloat("price_amount_micros") / 1000000).ToString();
            CurrencyCode = json.ToString("price_currency_code");

			SubscriptionPeriod = json.ToString("subscriptionPeriod");
			FreeTrialPeriod = json.ToString("freeTrialPeriod");

			IntroductoryPriceValue = (json.ToFloat("introductoryPriceAmountMicros") / 1000000).ToString();
			IntroductoryPrice = json.ToString("introductoryPrice");
			IntroductoryPricePeriod = json.ToString("introductoryPricePeriod");
			IntroductoryPriceCycles = json.ToString("introductoryPriceCycles");

			Sku = json.ToString("productId");
        }

        /**
         * ToString
         * @return formatted string
         */
        public override string ToString()
        {
            return string.Format("[SkuDetails: type = {0}, SKU = {1}, title = {2}, price = {3}, description = {4}, priceValue = {5}, currency = {6}, subscriptionPeriod = {7}, freeTrialPeriod = {8}, introductoryPrice = {9}, introductoryPricePeriod = {10}, introductoryPriceCycles = {11}]",
                                 ItemType, Sku, Title, Price, Description, PriceValue, CurrencyCode, SubscriptionPeriod, FreeTrialPeriod, IntroductoryPrice, IntroductoryPricePeriod, IntroductoryPriceCycles);
        }
    }
}
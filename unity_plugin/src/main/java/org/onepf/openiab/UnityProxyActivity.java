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

package org.onepf.openiab;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Bundle;
import android.util.Log;
import org.onepf.oms.appstore.googleUtils.IabHelper;
import org.onepf.oms.appstore.googleUtils.IabResult;

/**
 * Proxy activity is required to avoid making changes in the main Unity activity
 * It is created when purchase is started
 */
public class UnityProxyActivity extends Activity {
    static final String ACTION_FINISH = "org.onepf.openiab.ACTION_FINISH";
    private BroadcastReceiver broadcastReceiver;
    
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        broadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                Log.d(UnityPlugin.TAG, "Finish broadcast was received");
                if (!UnityProxyActivity.this.isFinishing()) {
                    finish();
                }
            }
        };
        registerReceiver(broadcastReceiver, new IntentFilter(ACTION_FINISH));

        if (UnityPlugin.sendRequest) {
            UnityPlugin.sendRequest = false;

            Intent i = getIntent();
            String sku = i.getStringExtra("sku");
            String developerPayload = i.getStringExtra("developerPayload");
            boolean inapp = i.getBooleanExtra("inapp", true);

            try {
                if (inapp) {
                    UnityPlugin.instance().getHelper().launchPurchaseFlow(this, sku, UnityPlugin.RC_REQUEST, UnityPlugin.instance().getPurchaseFinishedListener(), developerPayload);
                } else {
                    UnityPlugin.instance().getHelper().launchSubscriptionPurchaseFlow(this, sku, UnityPlugin.RC_REQUEST, UnityPlugin.instance().getPurchaseFinishedListener(), developerPayload);
                }
            } catch (java.lang.IllegalStateException e) {
                UnityPlugin.instance().getPurchaseFinishedListener().onIabPurchaseFinished(new IabResult(IabHelper.BILLING_RESPONSE_RESULT_BILLING_UNAVAILABLE, "Cannot start purchase process. Billing unavailable."), null);
            }
        }
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        unregisterReceiver(broadcastReceiver);
    }
    
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        Log.d(UnityPlugin.TAG, "onActivityResult(" + requestCode + ", " + resultCode + ", " + data);

        try {
            if (UnityPlugin.instance() == null) {
                Log.d(UnityPlugin.TAG, "onActivityResult UnityPlugin instance was null.");
                return;
            }

            if (UnityPlugin.instance().getHelper() == null) {
                Log.d(UnityPlugin.TAG, "onActivityResult helper was null, is billing not supported?");
                return;
            }

            // Pass on the activity result to the helper for handling
            if (!UnityPlugin.instance().getHelper().handleActivityResult(requestCode, resultCode, data)) {
                // not handled, so handle it ourselves (here's where you'd
                // perform any handling of activity results not related to in-app
                // billing...
                super.onActivityResult(requestCode, resultCode, data);
            } else {
                Log.d(UnityPlugin.TAG, "onActivityResult handled by IABUtil.");
            }
        } catch (java.lang.RuntimeException e){
            Log.d(UnityPlugin.TAG, "onActivityResult failure. Billing unavailable.");
        }
    }
}

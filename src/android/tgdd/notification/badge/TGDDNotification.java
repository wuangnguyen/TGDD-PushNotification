package tgdd.notification.badge;

import org.apache.cordova.*;
import org.json.JSONArray;
import org.json.JSONException;
import me.leolin.shortcutbadger.ShortcutBadgeException;
import me.leolin.shortcutbadger.ShortcutBadger;
import android.app.Activity;
import android.content.Context;

public class TGDDNotification extends CordovaPlugin {
  private   static CordovaWebView webView = null;
  protected static Context context = null;
  static Activity activity;
  @Override
  public void initialize (CordovaInterface cordova, CordovaWebView webView) {
      super.initialize(cordova, webView);

      TGDDNotification.webView = super.webView;
      TGDDNotification.context = super.cordova.getActivity().getApplicationContext();
      TGDDNotification.activity = super.cordova.getActivity();
  }
  @Override
  public boolean execute(String action, final JSONArray data, final CallbackContext callbackContext) throws JSONException {
      if (action.equals("setBadge")) {
      cordova.getThreadPool().execute(new Runnable() {
        public void run() {
          int badgeCount = 0;
          try {
            try {
              badgeCount = Integer.parseInt(data.getString(0));
            } catch (JSONException e) {
              // TODO Auto-generated catch block
              e.printStackTrace();
            }
          } catch (NumberFormatException e) {
          }
          try {
            ShortcutBadger.setBadge(context, badgeCount);
          } catch (ShortcutBadgeException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
          }
          callbackContext.success(); // Thread-safe.
        }
      });
      return true;
    } else {
      return false;
    }
  }
}

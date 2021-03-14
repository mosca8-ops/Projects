package it.unimib.turistafelice.utils;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import androidx.annotation.NonNull;

import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.firestore.DocumentSnapshot;
import com.google.firebase.firestore.FirebaseFirestore;
import com.google.firebase.firestore.QueryDocumentSnapshot;
import com.google.firebase.firestore.QuerySnapshot;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.Set;

public class RefreshUtil {
    private static final String TAG="RefreshUtil";

    public static void refreshSharedPreferences(FirebaseAuth fAuth, Context activity, Context context) {
        fAuth = FirebaseAuth.getInstance();
        FirebaseFirestore fStore = FirebaseFirestore.getInstance();

        String userId = fAuth.getCurrentUser().getUid();
        FirebaseUser firebaseUser = fAuth.getCurrentUser();
        fAuth.getCurrentUser().getDisplayName();

        Task<DocumentSnapshot> username = fStore.collection("users").document(userId)
                .get()
                .addOnCompleteListener(new OnCompleteListener<DocumentSnapshot>() {
                    @Override
                    public void onComplete(@NonNull Task<DocumentSnapshot> task) {
                        if (task.isSuccessful()) {
                            DocumentSnapshot document= task.getResult();

                            Log.d(TAG, "onComplete username: " + document.get("fName"));
                            SharedPreferences sharedPreferences = activity.getSharedPreferences(userId, context.MODE_PRIVATE);

                            SharedPreferences.Editor editor = sharedPreferences.edit();

                            editor.putString(Constants.FULL_NAME, document.getString("fName"));
                            editor.apply();
                        } else {
                            Log.d(TAG, "Error getting documents: ", task.getException());
                        }
                    }
                });

        Task<QuerySnapshot> allTripsReference = fStore.collection("users")
                .document(userId).collection(Constants.ALL_TRIPS).get()
                .addOnCompleteListener(new OnCompleteListener<QuerySnapshot>() {
                    @Override
                    public void onComplete(@NonNull Task<QuerySnapshot> task) {
                        if (task.isSuccessful()) {
                            for (QueryDocumentSnapshot document : task.getResult()) {
                                ArrayList<String> cityNames = (ArrayList<String>) document.getData().get(Constants.ALL_TRIPS);
                                getPlacesIdFromReferences(fStore, userId, cityNames,activity,context);
                                Set<String> cityNamesSet = new HashSet<String>(cityNames);
                                SharedPreferences sharedPreferences = activity.getSharedPreferences(userId, context.MODE_PRIVATE);
                                SharedPreferences.Editor editor = sharedPreferences.edit();
                                Log.d(TAG, "onComplete city names: " + cityNamesSet);
                                editor.putStringSet(Constants.ALL_TRIPS, cityNamesSet);
                                editor.apply();
                            }
                        } else {
                            Log.d(TAG, "Error getting documents: ", task.getException());
                        }
                    }
                });

        Task<QuerySnapshot> allInterests = fStore.collection("users")
                .document(userId).collection(Constants.ALL_USER_INTERESTS).get()
                .addOnCompleteListener(new OnCompleteListener<QuerySnapshot>() {
                    @Override
                    public void onComplete(@NonNull Task<QuerySnapshot> task) {
                        if (task.isSuccessful()) {
                            for (QueryDocumentSnapshot document : task.getResult()) {
                                ArrayList<String> interests = (ArrayList<String>) document.getData().get(Constants.ALL_USER_INTERESTS);
                                Set<String> interestsSet = new HashSet<String>(interests);

                                SharedPreferences sharedPreferences = activity.getSharedPreferences(userId, context.MODE_PRIVATE);
                                SharedPreferences.Editor editor = sharedPreferences.edit();
                                Log.d(TAG, "onComplete interests: " + interestsSet);
                                editor.putStringSet(Constants.ALL_USER_INTERESTS, interestsSet);
                                editor.apply();
                            }
                        } else {
                            Log.d(TAG, "Error getting documents: ", task.getException());
                        }
                    }
                });
    }

    private static void getPlacesIdFromReferences(FirebaseFirestore fStore, String userId, ArrayList<String> citiesNames, Context activity, Context context) {
        Task<QuerySnapshot> allPlacesIdReference;
        for(String cityName : citiesNames){
            allPlacesIdReference = fStore.collection("users")
                    .document(userId).collection(cityName).get()
                    .addOnCompleteListener(new OnCompleteListener<QuerySnapshot>() {
                        @Override
                        public void onComplete(@NonNull Task<QuerySnapshot> task) {
                            if (task.isSuccessful()) {
                                for (QueryDocumentSnapshot document : task.getResult()) {
                                    ArrayList<String> placesId = (ArrayList<String>) document.getData().get(cityName);
                                    getPlacesIdFromReferences(fStore, userId, placesId,context, activity);
                                    Set<String> placesIdSet = new HashSet<String>(placesId);
                                    SharedPreferences sharedPreferences = activity.getSharedPreferences(userId, context.MODE_PRIVATE);
                                    SharedPreferences.Editor editor = sharedPreferences.edit();
                                    Log.d(TAG, "onComplete places Id: " + placesIdSet);
                                    editor.putStringSet(cityName, placesIdSet);
                                    editor.apply();
                                }
                            } else {
                                Log.d(TAG, "Error getting documents: ", task.getException());
                            }
                        }
                    });
        }
    }

}

package it.unimib.turistafelice.adapter;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.ImageView;
import android.widget.ProgressBar;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.bumptech.glide.Glide;
import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.firestore.DocumentReference;
import com.google.firebase.firestore.FirebaseFirestore;
import com.squareup.picasso.Callback;
import com.squareup.picasso.Picasso;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.R;
import it.unimib.turistafelice.model.Place;
import it.unimib.turistafelice.utils.Constants;

public class PlaceAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder>{

    private static final int PLACE_VIEW_TYPE = 0;
    private static final int LOADING_VIEW_TYPE = 1;



    public interface OnItemClickListener{
        void onItemClick(Place place);
    }

    private static final String TAG ="PlaceAdapter";
    private List<Place> places;
    private LayoutInflater layoutInflater;
    private OnItemClickListener onItemClickListener;
    private String city;
    private Context context;




    public PlaceAdapter(List<Place> places, String city, Context context, OnItemClickListener onItemClickListener) {
        this.places = places;
        this.layoutInflater = LayoutInflater.from(context);
        this.onItemClickListener = onItemClickListener;
        this.city=city;
        this.context=context;
    }


    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        if (viewType == PLACE_VIEW_TYPE) {
            View view = this.layoutInflater.inflate(R.layout.place_item, parent, false);
            return new PlaceViewHolder(view);
        } else {
            View view = this.layoutInflater.inflate(R.layout.loading_item, parent, false);
            return new LoadingNewsViewHolder(view);
        }
    }

    @Override
    public void onViewRecycled(RecyclerView.ViewHolder holder) {
        ((PlaceViewHolder) holder).setCheckBoxFavorites(city);
        super.onViewRecycled(holder);
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {
        if (holder instanceof PlaceViewHolder) {
            ((PlaceViewHolder) holder).bind(places.get(position), city, this.onItemClickListener);
        } else if (holder instanceof LoadingNewsViewHolder) {
            ((LoadingNewsViewHolder) holder).progressBarLoadingNews.setIndeterminate(true);
        }
    }

    @Override
    public int getItemCount() {
        if (places != null) {
            return places.size();
        }
        return 0;
    }

    @Override
    public int getItemViewType(int position) {
        if (places.get(position) == null /*|| !(isImgLoaded)*/) {
            return LOADING_VIEW_TYPE;
        } else {
            return PLACE_VIEW_TYPE;
        }
    }

    public void setData(List<Place> places) {
        if (places != null) {
            this.places = places;
            notifyDataSetChanged();
        }
    }

    public  class PlaceViewHolder extends RecyclerView.ViewHolder {
        TextView placeName;
        TextView placeAddress;
        ImageView imageViewPlace;
        CheckBox checkBoxFavorite;
        Map<String, Object> userFavoritesDoc = new HashMap<>();
        Map<String, Object> userTripsDoc = new HashMap<>();
        DocumentReference documentReferenceInterests;
        FirebaseAuth fAuth =  FirebaseAuth.getInstance();
        FirebaseFirestore fStore = FirebaseFirestore.getInstance();
        Set<String> userTripsSet = new HashSet<>();
        Set<String> userFavoritesSet = new HashSet<>();
        SharedPreferences sharedPreferences;
        String userId = fAuth.getCurrentUser().getUid();
        DocumentReference documentReference = fStore.collection(Constants.USER)
                .document(userId).collection(Constants.ALL_TRIPS).document(Constants.ALL_TRIPS);
        SharedPreferences.Editor editor;
        public String placeID;
        public String city;

        public PlaceViewHolder(View v) {
            super(v);
            sharedPreferences = context.getSharedPreferences(userId, context.MODE_PRIVATE);            SharedPreferences.Editor editor = sharedPreferences.edit();

            userTripsSet = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);
            placeName = v.findViewById(R.id.place_name_id);
            placeAddress = v.findViewById(R.id.place_description);
            imageViewPlace = v.findViewById(R.id.image_view_place);
            checkBoxFavorite = v.findViewById(R.id.icon_favorite);
        }



        public void bind(Place place, String city, OnItemClickListener onItemClickListener) {
            editor = sharedPreferences.edit();
            placeID = place.getPlace_id();
            setPhotoForPlace(place);
            placeName.setText(place.getName());
            placeAddress.setText(place.getFormatted_address());
            setCheckBoxFavorites(city);

            itemView.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    onItemClickListener.onItemClick(place);
                }
            });
            checkBoxFavorite.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    userTripsSet = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);
                    if (checkBoxFavorite.isChecked()) {
                        if (!userTripsSet.contains(city)) { //aggiungo place id nel caso in cui non c'è ancora la città

                            ArrayList<String> userTrips = new ArrayList<>();
                            userTripsSet.add(city);
                            userTrips.addAll(userTripsSet);
                            userTripsDoc.put(Constants.ALL_TRIPS, userTrips);
                            documentReference.update(userTripsDoc);
                            editor.remove(Constants.ALL_TRIPS);
                            editor.putStringSet(Constants.ALL_TRIPS, userTripsSet);
                            editor.apply();
                        }else{
                            Log.d(TAG, "onClick: " + sharedPreferences.getStringSet(city, null));
                            userFavoritesSet = sharedPreferences.getStringSet(city, null);
                        }
                        userFavoritesSet.add(placeID);
                        ArrayList<String> userFavorites = new ArrayList<>();
                        userFavorites.addAll(userFavoritesSet);
                        userFavoritesDoc.put(city, userFavorites);
                        documentReferenceInterests = fStore.collection(Constants.USER)
                                .document(userId).collection(city).document(city);
                        documentReferenceInterests.set(userFavoritesDoc);
                        Log.d(TAG, "onClick: " + city + "  " + userFavoritesSet);
                        editor.remove(city);
                        editor.apply();

                        editor.putStringSet(city, userFavoritesSet);
                        editor.apply();
                        Snackbar.make(v, R.string.place_added, Snackbar.LENGTH_SHORT).show();
                    }else{
                        userFavoritesSet = sharedPreferences.getStringSet(city, null);
                        userFavoritesSet.remove(placeID);
                        ArrayList<String> allTripPlaceList = new ArrayList<>();
                        allTripPlaceList.addAll(userFavoritesSet);
                        userFavoritesDoc.put(city, allTripPlaceList);

                        documentReferenceInterests = fStore.collection(Constants.USER)
                                .document(userId).collection(city).document(city);
                        documentReferenceInterests.update(userFavoritesDoc);

                        editor.remove(city);
                        editor.apply();
                        editor.putStringSet(city, userFavoritesSet);
                        editor.apply();
                        if(userFavoritesSet.isEmpty()){
                            Log.d(TAG, ""+ userTripsSet);
                            Log.d(TAG, city);
                            userTripsSet.remove(city);

                            ArrayList<String> allTripList = new ArrayList<>();
                            allTripList.addAll(userTripsSet);
                            userTripsDoc.put(Constants.ALL_TRIPS, allTripList);
                            documentReference.update(userTripsDoc);
                            editor.remove(city);
                            editor.apply();
                            documentReferenceInterests.delete();
                        }
                        Snackbar.make(v, R.string.place_removed, Snackbar.LENGTH_SHORT).show();
                    }
                }
            });
        }

        public void setCheckBoxFavorites(String city) {
            Log.d(TAG, "setCheckBoxFavorites: " + city + "   " + placeID);
            if (userTripsSet.contains(city) ) {         //controllo se il cuoricino deve essere checked o meno
                userFavoritesSet = sharedPreferences.getStringSet(city, null);
                Log.d(TAG, "onViewCreated: " + userFavoritesSet);
                if(userFavoritesSet.contains(placeID)){
                    checkBoxFavorite.setChecked(true);
                }else{
                    checkBoxFavorite.setChecked(false);
                }
            }
        }

        private void setPhotoForPlace(Place place) {
            if(place.getPhotos() != null){
                if (place.getPhotos().size() > 0) {
                    Picasso.get()
                            .load(getPath(place))
                            .fit()
                            .into(imageViewPlace, new Callback() {
                                @Override
                                public void onSuccess() {
                                    Log.d(TAG, "Ok"+ place.getName());
                                    //isImgLoaded = true;
                                }
                                @Override
                                public void onError(Exception e) {
                                    Log.d(TAG, "onError: "+ e.getMessage());
                                    Glide.with(itemView.getContext()).load(getPath(place)).into(imageViewPlace);
                                }
                            });
                }
            }
            else{
                Log.d(TAG, "No photo");
            }
        }

        private String getPath(Place place) {
            String Path = Constants.PLACE_PHOTO_API_URL + "photoreference="
                    + place.getPhotos().get(0).getPhoto_reference()
                    + "&maxwidth=" + place.getPhotos().get(0).getWidth()
                    + "&key=" + Constants.API_KEY;
            Log.d(TAG, "getPath: "+Path);
            return Path;
        }
    }

    static class LoadingNewsViewHolder extends RecyclerView.ViewHolder {

        ProgressBar progressBarLoadingNews;

        LoadingNewsViewHolder(View view) {
            super(view);
            progressBarLoadingNews = view.findViewById(R.id.progressBarLoadingNews);
        }
    }
}

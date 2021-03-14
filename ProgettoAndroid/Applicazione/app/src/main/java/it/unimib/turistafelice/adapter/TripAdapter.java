package it.unimib.turistafelice.adapter;

import android.content.Context;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.squareup.picasso.Picasso;

import java.util.List;
import java.util.Map;

import it.unimib.turistafelice.R;
import it.unimib.turistafelice.model.Trip;
import it.unimib.turistafelice.viewmodel.UnsplashViewModel;

public class TripAdapter extends RecyclerView.Adapter<TripAdapter.TripViewHolder> {

    private static final String TAG = "TripAdapter";
    private List<Trip> trips;
    private LayoutInflater layoutInflater;
    private OnItemClickListener onItemClickListener;
    private UnsplashViewModel unsplashViewModel;

    @NonNull
    @Override
    public TripViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = this.layoutInflater.inflate(R.layout.city_trip_item, parent, false);
        return new TripViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull TripViewHolder holder, int position) {
        holder.bind(trips.get(position), this.onItemClickListener);
    }

    @Override
    public int getItemCount() {
        return trips.size();
    }


    public interface OnItemClickListener {
        void onItemClick(Trip cityTripName);
    }

    public TripAdapter(List<Trip> trips, Context context,TripAdapter.OnItemClickListener onItemClickListener) {
        this.trips = trips;
        this.layoutInflater = LayoutInflater.from(context);
        this.onItemClickListener = onItemClickListener;
    }

    public void addUrl(Map<String, String> mapUrls) {
        for (Map.Entry<String,String> entry : mapUrls.entrySet()){
            for (int i = 0; i < trips.size(); i++){
                if(trips.get(i).getName().equals(entry.getKey())){
                    if(trips.get(i).getUrlPhoto() == null) {
                        trips.get(i).setUrlPhoto(entry.getValue());
                        notifyDataSetChanged();
                    }
                }
            }
        }
    }

    public static class TripViewHolder extends RecyclerView.ViewHolder {

        TextView cityTripName;
        ImageView cityImage;

        public TripViewHolder(View v) {
            super(v);
            cityTripName = v.findViewById(R.id.text_city_trip_name);
            cityImage = v.findViewById(R.id.image_city_trip_fav);
        }

        public void bind(Trip trip, OnItemClickListener onItemClickListener) {

            cityTripName.setText(trip.getName());
            //chiamata api cityImage...
            Picasso.get().load(trip.getUrlPhoto()).fit().into(cityImage);
            Log.d(TAG, "url "+ trip.getUrlPhoto() + " " +trip.getName());

            itemView.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    Log.d(TAG, "onClick: "+trip.getName());
                    onItemClickListener.onItemClick(trip);
                }
            });
        }
    }
}

package it.unimib.turistafelice;

import android.annotation.SuppressLint;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.provider.MediaStore;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.navigation.Navigation;

import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.AuthCredential;
import com.google.firebase.auth.EmailAuthProvider;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.firestore.DocumentReference;
import com.google.firebase.firestore.DocumentSnapshot;
import com.google.firebase.firestore.EventListener;
import com.google.firebase.firestore.FirebaseFirestore;
import com.google.firebase.firestore.FirebaseFirestoreException;
import com.google.firebase.storage.FirebaseStorage;
import com.google.firebase.storage.StorageReference;
import com.google.firebase.storage.UploadTask;
import com.squareup.picasso.Picasso;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.databinding.FragmentProfileBinding;
import it.unimib.turistafelice.utils.Constants;

public class ProfileFragment extends Fragment {

    private String TAG ="Profile Fragment";
    private FragmentProfileBinding binding;
    private FirebaseAuth fAuth;
    private FirebaseFirestore fStore;
    private FirebaseUser user;
    private String userId;
    private StorageReference storageReference;
    private Boolean logged = true;

    public ProfileFragment() {
        // Required empty public constructor
    }


    public static ProfileFragment newInstance(String param1, String param2) {
        ProfileFragment fragment = new ProfileFragment();
        return fragment;
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        binding = binding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        binding.saveButton.setVisibility(View.GONE);
        binding.buttonChangePassword.setVisibility(View.GONE);
        binding.progressBarImageChange.setVisibility(View.GONE);

        fAuth = FirebaseAuth.getInstance();
        fStore = FirebaseFirestore.getInstance();
        userId = fAuth.getCurrentUser().getUid();
        storageReference = FirebaseStorage.getInstance().getReference();
        Context context = getContext();
        SharedPreferences  sharedPreferences = context.getSharedPreferences(userId, context.MODE_PRIVATE);

        if(fAuth.getCurrentUser()!=null) {
            StorageReference profileRef = storageReference
                    .child("users/" +fAuth.getCurrentUser().getUid() + "/profile.jpg");
            profileRef.getDownloadUrl().addOnSuccessListener(new OnSuccessListener<Uri>() {
                @Override
                public void onSuccess(Uri uri) {
                    Picasso.get().load(uri).into(binding.profileImageView);
                }
            }).addOnFailureListener(new OnFailureListener() {
                @Override
                public void onFailure(@NonNull Exception e) {
                    Snackbar.make(view, getResources().getString(R.string.no_profile_image), Snackbar.LENGTH_SHORT).show();
                }
            });
            //vediamo come leggere dati dal db
            //colleghiamo l oggetto docRef al documento del db con userId del currentUser
            //snapghotListener guarda anche i cambiamenti del documento, sarà utile in caso l utente cambiasse i parametri
            DocumentReference documentReference = fStore
                    .collection(Constants.USER).document(userId);
            documentReference
                    .addSnapshotListener(this.requireActivity(), new EventListener<DocumentSnapshot>() {
                        @Override
                        public void onEvent(@Nullable DocumentSnapshot documentSnapshot,
                                            @Nullable FirebaseFirestoreException e) {
                            if( documentSnapshot != null) {
                                //qui utiliziamo l obj documentSnapshot e la chiave con cui abbiamo salvato i valori nel db precedentemente
                                binding.profilePhone.setText(documentSnapshot.getString(Constants.PHONE));
                                binding.profileName.setText(documentSnapshot.getString(Constants.FNAME));
                                binding.profileEmail.setText(documentSnapshot.getString(Constants.EMAIL));
                                binding.favoriteCityTextView.setText(documentSnapshot.getString(Constants.FAVCITY));
                            }
                        }
                    });
        }

        Set<String> travelMade = sharedPreferences.getStringSet(Constants.ALL_TRIPS, null);
        Log.d(TAG, "" + travelMade);
        int nVisitedPlaces = 0;
        if(travelMade != null){

            int nTravelMade = (int) travelMade.size();
            binding.travelMadeTextView.setText(String.valueOf(nTravelMade));

            ArrayList<String> allTripMade = new ArrayList<>();
            allTripMade.addAll(travelMade);
            Log.d(TAG, "" + allTripMade);
            if (allTripMade != null ){
                for(int i = 0; i < allTripMade.size(); i++) {
                    Log.d(TAG, allTripMade.get(i));
                    int nPlacesPerTrip = sharedPreferences.getStringSet(allTripMade.get(i), null).size();
                    nVisitedPlaces += nPlacesPerTrip;
                }
            }
        }else
            binding.travelMadeTextView.setText(String.valueOf(0));

        binding.visitedPlacesTextView.setText(String.valueOf(nVisitedPlaces));

        binding.yourInterestsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                ProfileFragmentDirections.ShowInterestsAction showInterestsAction =
                        ProfileFragmentDirections.showInterestsAction();
                Navigation.findNavController(view).navigate(showInterestsAction);
            }
        });

        binding.logoutButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                logged = false;
                ProfileFragmentDirections
                        .LogoutAction logoutAction = ProfileFragmentDirections.logoutAction();
                Navigation.findNavController(view).navigate(logoutAction);
                onDestroyView();
                onDestroy();
                fAuth.signOut();
            }
        });


        binding.button.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                binding.profileImageView.setClickable(true);
                binding.profileName.setClickable(true);
                binding.profilePhone.setClickable(true);
                binding.profileEmail.setClickable(true);

                binding.yourInterestsButton.setVisibility(View.GONE);
                binding.button.setVisibility(View.GONE);
                binding.logoutButton.setVisibility(View.GONE);
                binding.saveButton.setVisibility(View.VISIBLE);
                binding.buttonChangePassword.setVisibility(View.VISIBLE);

                Snackbar.make(view, getResources().getString(R.string.click_on_what_you_change), Snackbar.LENGTH_SHORT).show();

                binding.favoriteCityLinearLayout.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        final EditText newCity = new EditText(view.getContext());
                        AlertDialog.Builder changeNameDialog =
                                new AlertDialog.Builder(view.getContext());
                        changeNameDialog.setTitle(getResources().getString(R.string.change_fav_city));
                        changeNameDialog.setMessage(getResources().getString(R.string.insert_new_fav_city));
                        changeNameDialog.setView(newCity);
                        changeNameDialog
                                .setPositiveButton("Ok",
                                        new DialogInterface.OnClickListener() {
                                            @SuppressLint("SetTextI18n")
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                if (newCity.getText().toString().isEmpty()) {
                                                    Snackbar.make(view,
                                                            getResources().getString(R.string.field_cant_be_empty),
                                                            Snackbar.LENGTH_SHORT).show();
                                                    dialogInterface.dismiss();
                                                } else {
                                                    binding.favoriteCityTextView.setText(newCity.getText().toString());
                                                }
                                            }
                                        });
                        changeNameDialog
                                .setNegativeButton(getResources().getString(R.string.annulla),
                                        new DialogInterface.OnClickListener() {
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                dialogInterface.dismiss();
                                            }
                                        });
                        changeNameDialog.show();
                    }
                });

                binding.profileImageView.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {
                        //open gallery
                        Intent openGalleryIntent = new Intent(Intent.ACTION_PICK,
                                MediaStore.Images.Media.EXTERNAL_CONTENT_URI);
                        startActivityForResult(openGalleryIntent, 1000);
                    }
                });


                binding.profileName.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {
                        Log.d(TAG, "cliccato");
                        final EditText newName = new EditText(view.getContext());
                        AlertDialog.Builder changeNameDialog =
                                new AlertDialog.Builder(view.getContext());
                        changeNameDialog.setTitle(getResources().getString(R.string.change_name));
                        changeNameDialog.setMessage(getResources().getString(R.string.insert_new_name));
                        changeNameDialog.setView(newName);
                        changeNameDialog
                                .setPositiveButton("Ok",
                                        new DialogInterface.OnClickListener() {
                                            @SuppressLint("SetTextI18n")
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                if (newName.getText().toString().isEmpty()) {
                                                    Snackbar.make(view,
                                                            getResources().getString(R.string.field_cant_be_empty),
                                                            Snackbar.LENGTH_SHORT).show();
                                                    dialogInterface.dismiss();
                                                } else {
                                                    binding.profileName.setText(newName.getText().toString());
                                                }
                                            }
                                        });
                        changeNameDialog
                                .setNegativeButton(getResources().getString(R.string.annulla),
                                        new DialogInterface.OnClickListener() {
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                dialogInterface.dismiss();
                                            }
                                        });
                        changeNameDialog.show();
                    }
                });


                binding.profilePhone.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {
                        Log.d("ProfileFragment", "cliccato");
                        final EditText newPhone = new EditText(view.getContext());
                        AlertDialog.Builder changePhoneDialog =
                                new AlertDialog.Builder(view.getContext());
                        changePhoneDialog.setTitle(getResources().getString(R.string.change_phone_number));
                        changePhoneDialog.setMessage(getResources().getString(R.string.insert_new_phone_number));
                        changePhoneDialog.setView(newPhone);
                        changePhoneDialog
                                .setPositiveButton("Ok",
                                        new DialogInterface.OnClickListener() {
                                            @SuppressLint("SetTextI18n")
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                if (newPhone.getText().toString().isEmpty()) {
                                                    Snackbar.make(view,
                                                            getResources().getString(R.string.field_cant_be_empty),
                                                            Snackbar.LENGTH_SHORT).show();
                                                    dialogInterface.dismiss();
                                                } else {
                                                    if(newPhone.getText().toString().length()>6
                                                            && newPhone.getText().toString().length()<12){
                                                        try {
                                                            if (Integer.parseInt(newPhone.getText().toString()) > 100000) {
                                                                binding.profilePhone.setText(newPhone.getText().toString());
                                                            }
                                                        } catch (Exception e){
                                                            Snackbar.make(view,
                                                                    "Error: " + e.getMessage(),
                                                                    Snackbar.LENGTH_SHORT).show();
                                                        }
                                                    } else {
                                                        Snackbar.make(view,
                                                                R.string.invalid_phone,
                                                                Snackbar.LENGTH_SHORT).show();
                                                    }

                                                }
                                            }
                                        });
                        changePhoneDialog
                                .setNegativeButton(getResources().getString(R.string.annulla),
                                        new DialogInterface.OnClickListener() {
                                            @Override
                                            public void onClick(DialogInterface dialogInterface, int i) {
                                                dialogInterface.dismiss();
                                            }
                                        });
                        changePhoneDialog.show();
                    }
                });

                binding.buttonChangePassword.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {

                        final EditText reAuth = new EditText(view.getContext());
                        AlertDialog.Builder reAuthDialog =
                                new AlertDialog.Builder(view.getContext());
                        reAuthDialog.setTitle(R.string.change_password);
                        reAuthDialog
                                .setMessage(getResources().getString(R.string.insert_old_pw));
                        reAuthDialog.setView(reAuth);
                        reAuthDialog.setPositiveButton("Ok", new DialogInterface.OnClickListener(){
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                AuthCredential credential = EmailAuthProvider
                                        .getCredential(fAuth.getCurrentUser().getEmail(),
                                                reAuth.getText().toString());

                                FirebaseAuth.getInstance()
                                        .getCurrentUser()
                                        .reauthenticate(credential)
                                        .addOnSuccessListener(new OnSuccessListener<Void>() {
                                            @Override
                                            public void onSuccess(Void aVoid) {
                                                Snackbar.make(view, getResources().getString(R.string.validated_user),
                                                        Snackbar.LENGTH_SHORT).show();

                                                final EditText changePassword = new EditText(view.getContext());
                                                AlertDialog.Builder changePasswordDialog =
                                                        new AlertDialog.Builder(view.getContext());
                                                changePasswordDialog.setTitle(getResources().getString(R.string.change_password));
                                                changePasswordDialog
                                                        .setMessage(getResources().getString(R.string.insert_new_psw));
                                                changePasswordDialog.setView(changePassword);
                                                changePasswordDialog
                                                        .setPositiveButton(getResources().getString(R.string.change),
                                                                new DialogInterface.OnClickListener() {
                                                                    @Override
                                                                    public void onClick(DialogInterface dialogInterface, int i) {
                                                                        if (changePassword.getText().toString().isEmpty()
                                                                                || changePassword.getText().toString().length() < 6) {
                                                                            Snackbar.make(view,
                                                                                    getResources().getString(R.string.invalid_psw),
                                                                                    Snackbar.LENGTH_SHORT).show();
                                                                        } else {
                                                                            fAuth.getCurrentUser()
                                                                                    .updatePassword(changePassword.getText().toString())
                                                                                    .addOnSuccessListener(new OnSuccessListener<Void>() {
                                                                                        @Override
                                                                                        public void onSuccess(Void aVoid) {
                                                                                            Snackbar.make(view,
                                                                                                    getResources().getString(R.string.psw_changed),
                                                                                                    Snackbar.LENGTH_SHORT).show();
                                                                                        }
                                                                                    }).addOnFailureListener(new OnFailureListener() {
                                                                                @Override
                                                                                public void onFailure(@NonNull Exception e) {
                                                                                    Snackbar.make(view,
                                                                                            "Error: " +
                                                                                                    e.getMessage(),
                                                                                            Snackbar.LENGTH_SHORT).show();
                                                                                }
                                                                            });
                                                                        }
                                                                    }
                                                                });
                                                changePasswordDialog
                                                        .setNegativeButton(getResources().getString(R.string.annulla),
                                                                new DialogInterface.OnClickListener() {
                                                                    @Override
                                                                    public void onClick(DialogInterface dialogInterface, int i) {
                                                                        dialogInterface.dismiss();
                                                                    }
                                                                });

                                                changePasswordDialog.show();
                                            }
                                        }).addOnFailureListener(new OnFailureListener() {
                                    @Override
                                    public void onFailure(@NonNull Exception e) {
                                        Snackbar.make(view, getResources().getString(R.string.user_not_riautenticated), Snackbar.LENGTH_SHORT).show();
                                    }
                                });
                            }
                        });

                        reAuthDialog.setNegativeButton(getResources().getString(R.string.annulla), new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                dialog.dismiss();
                            }
                        });

                        reAuthDialog.show();



                    }
                });


                binding.saveButton.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {
                        Log.d("ProfileFragment", "cliccato save");

                        binding.profileImageView.setClickable(false);
                        binding.profileName.setClickable(false);
                        binding.profilePhone.setClickable(false);
                        binding.profileEmail.setClickable(false);

                        if (binding.profilePhone.getText().toString().isEmpty()
                                || binding.profileName.getText().toString().isEmpty()
                                || binding.profileEmail.getText().toString().isEmpty()) {
                            Snackbar.make(view,
                                    getResources().getString(R.string.invalid_field),
                                    Snackbar.LENGTH_SHORT).show();
                        } else {
                            fStore = FirebaseFirestore.getInstance();
                            DocumentReference documentReference =
                                    fStore.collection(Constants.USER)
                                            .document(fAuth.getCurrentUser().getUid());

                            Map<String, Object> edited = new HashMap<>();
                            /*edited.put("email",
                                    binding.profileEmail.getText().toString());*/
                            edited.put(Constants.PHONE,
                                    binding.profilePhone.getText().toString());
                            edited.put(Constants.FNAME,
                                    binding.profileName.getText().toString());
                            edited.put(Constants.FAVCITY, binding.favoriteCityTextView.getText().toString());



                            documentReference
                                    .update(edited)
                                    .addOnFailureListener(new OnFailureListener() {
                                        @Override
                                        public void onFailure(@NonNull Exception e) {
                                            Snackbar.make(view,
                                                    "Error: " + e.getMessage(),
                                                    Snackbar.LENGTH_SHORT).show();
                                        }
                                    });
                            Snackbar.make(view,
                                    getResources().getString(R.string.saved_profile),
                                    Snackbar.LENGTH_SHORT).show();


                            DocumentReference documentReferenceRead = fStore
                                    .collection(Constants.USER).document(userId);
                            documentReferenceRead
                                    .addSnapshotListener(requireActivity(),
                                            new EventListener<DocumentSnapshot>() {
                                                @Override
                                                public void onEvent(@Nullable DocumentSnapshot docSnapshot,
                                                                    @Nullable FirebaseFirestoreException e) {
                                                    //qui utiliziamo l obj documentSnapshot e la chiave con cui abbiamo salvato i valori nel db precedentemente
                                                    try {
                                                        binding.profilePhone
                                                                .setText(docSnapshot.getString(Constants.PHONE));
                                                        binding.profileName
                                                                .setText(docSnapshot.getString(Constants.FNAME));
                                            /*binding.profileEmail
                                                    .setText(docSnapshot.getString(Constants.EMAIL));*/
                                                        binding.favoriteCityTextView
                                                                .setText(docSnapshot.getString((Constants.FAVCITY)));

                                                    }catch(Exception ex){
                                                        Log.e(TAG, "onEvent: ", ex);
                                                    }
                                                }
                                            });

                            String fullName = binding.profileName.getText().toString();
                            SharedPreferences.Editor editor = sharedPreferences.edit();
                            editor.putString(Constants.FULL_NAME, fullName);
                            editor.apply();
                            binding.yourInterestsButton.setVisibility(View.VISIBLE);
                            binding.button.setVisibility(View.VISIBLE);
                            binding.logoutButton.setVisibility(View.VISIBLE);
                            binding.saveButton.setVisibility(View.GONE);
                            binding.buttonChangePassword.setVisibility(View.GONE);
                        }
                    }
                });
            }
        });
        binding.profileImageView.setClickable(false);
        binding.profileName.setClickable(false);
        binding.profilePhone.setClickable(false);
        binding.profileEmail.setClickable(false);
    }


    @Override
    public void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == 1000){
            if (resultCode == this.requireActivity().RESULT_OK){
                assert data != null;
                Uri imageUri = data.getData();
                uploadImageToFirebase(imageUri);
                binding.progressBarImageChange.setVisibility(View.VISIBLE);
                binding.progressBarImageChange.getProgress();
            }
        }
    }


    private void uploadImageToFirebase(Uri imageUri) {
        //upload image to firebase
        StorageReference fileRef = storageReference
                .child("users/" +fAuth.getCurrentUser().getUid() + "/profile.jpg");
        fileRef.putFile(imageUri)
                .addOnSuccessListener(new OnSuccessListener<UploadTask.TaskSnapshot>() {
                    @Override
                    public void onSuccess(UploadTask.TaskSnapshot taskSnapshot) {
                        fileRef.getDownloadUrl().addOnSuccessListener(new OnSuccessListener<Uri>() {
                            @Override
                            public void onSuccess(Uri uri) {
                                Picasso.get().load(uri).into(binding.profileImageView);
                            }
                        });
                        binding.progressBarImageChange.setVisibility(View.GONE);
                        Snackbar.make(requireView(),
                                getResources().getString(R.string.uploaded_image),
                                Snackbar.LENGTH_SHORT).show();
                    }
                }).addOnFailureListener(new OnFailureListener() {
            @Override
            public void onFailure(@NonNull Exception e) {
                binding.progressBarImageChange.setVisibility(View.GONE);
                Snackbar.make(requireView(),
                        getResources().getString(R.string.failed),
                        Snackbar.LENGTH_SHORT).show();
            }
        });
    }
}

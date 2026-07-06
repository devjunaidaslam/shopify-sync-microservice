using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities
{
    public class ResponseMessageList
    {
		public string UserPhotoUploadMessage = "User photo uploaded successfully.";
		public string UserNotFoundResponseMessage = "User not found.";
        public string ResetPasswordMailSendSuccessMessage = "Reset password mail sent successfully.";
        public string FailResponseMessage = "Oops, something went wrong.";
        public string InvalidEmailOrPassword = "Invalid email or password.";    
        public string InvalidPasswordMessage = "Current password is incorrect.";
        public string LoginSuccess = "User logged in successfully.";
		public string LoginOutSuccessMessage = "Logout Successfully.";
		public string AccountAlreadyExist = "It looks like you already have an account associated with this email. Log in instead or reset your password if you've forgotten it.";
        public string UserRegisteredSuccessMessage = "User registered successfully.";
        public string DataNotProper = "Please enter proper data.";
        public string UserFailedToRegister = "User failed to register.";
        public string PasswordLength = "Password must be at least 12 characters long.";
        public string InvalidImagefile = "Invalid image file.";
        public string UserCreateMessage = "The user has been created successfully.";
        public string FailedToChangePasswordMessage = "Failed to change password.";
        public string PasswordChangeMessage = "Password reset successfully.";
		public string PasswordChangedMessage = "Password changed successfully.";
		public string UserFetchSuccessMessage = "User details fetched successfully.";
        public string ForbiddenAccessToViewUserMessage = "You are not authorized to view user details.";
        public string ForbiddenAccessToCreateUserMessage = "You are not authorized to create a user.";
        public string ForbiddenAccessToEditUserMessage = "You are not authorized to edit a user.";
        public string FailedToUpdateUserProfileMessage = "Failed to update user profile.";
        public string UserProfileUpdatedMessage = "User profile updated successfully.";
        public string PasswordNotMatchedMessage = "New password and confirmed password do not match.";
        public string InvalidTokenMessage = "Invalid token.";
        public string InvalidRefreshTokenMessage = "Invalid refresh token.";
        public string RefreshTokenGeneratedMessage = "Refresh token generated successfully.";
        public string UserActiveStatusMessage = "User is not active.";
        public string FailedToUpdateUserActiveStatusMessage = "Failed to update user active status.";
        public string UpdateUserActiveStatusMessage = "User status updated successfully.";
        public string UploadProperFileMessage = "Please upload a proper file.";
        public string FileUploadedSuccessMessage = "File uploaded successfully.";
        public string FileUploadedErrorMessage = "An error occurred while uploading the file.";
        public string UploadedFileNotProperErrorMessage = "Uploaded file is not in a proper format.";

        public string ProcessFileMessage = "All files imported to the temporary vehicle table.";

        public string ProperTypeIdMessage = "Please enter a valid type.";
        public string ProperModelIdMessage = "Please enter a valid model.";
        public string ProperMakeIdMessage = "Please enter a valid make.";
        public string ProperYearIdMessage = "Please enter a valid year.";
        public string InvalidUserRoleMessage = "Please provide a valid user role.";

        #region Vehicle Year Message
        public string FetchVehicleYearDetailsMessage = "Vehicle year details fetched successfully.";
        public string VehicleYearDetailsAddedMessage = "Vehicle year details added successfully.";
        public string VehicleYearDetailsUpdatedMessage = "Vehicle year details updated successfully.";
        public string VehicleYearDetailsRemoveMessage = "Vehicle year details removed successfully.";
        public string VehicleYearDetailsNotFoundMessage = "Vehicle year details not found.";
        #endregion

        #region Vehicle Make Message
        public string FetchVehicleMakeDetailsMessage = "Vehicle make details fetched successfully.";
        public string VehicleMakeDetailsAddedMessage = "Vehicle make details added successfully.";
        public string VehicleMakeDetailsUpdateMessage = "Vehicle make details updated successfully.";
        public string VehicleMakeDetailsRemoveMessage = "Vehicle make details removed successfully.";
        public string VehicleMakeDetailsNotFoundMessage = "Vehicle make details not found.";
        #endregion

        #region Location Message
        public string FetchLocationMessage = "Location fetched successfully.";
        public string LocationAddedMessage = "Location added successfully.";
        public string LocationUpdateMessage = "Location updated successfully.";
        public string LocationRemoveMessage = "Location removed successfully.";
        public string LocationNotFoundMessage = "Location not found.";
        #endregion

        #region Vendor Message
        public string FetchVendorMessage = "Vendor fetched successfully.";
        public string VendorAddedMessage = "Vendor added successfully.";
        public string VendorUpdateMessage = "Vendor updated successfully.";
        public string VendorRemoveMessage = "Vendor removed successfully.";
        public string VendorNotFoundMessage = "Vendor not found.";
        #endregion

        #region Tag Message
        public string FetchTagMessage = "Tag fetched successfully.";
        public string TagAddedMessage = "Tag added successfully.";
        public string TagUpdateMessage = "Tag updated successfully.";
        public string TagRemoveMessage = "Tag removed successfully.";
        public string TagNotFoundMessage = "Tag not found.";
        #endregion

        #region Collection Message
        public string FetchCollectionMessage = "Collection fetched successfully.";
        public string CollectionAddedMessage = "Collection added successfully.";
        public string CollectionUpdateMessage = "Collection updated successfully.";
        public string CollectionRemoveMessage = "Collection removed successfully.";
        public string CollectionNotFoundMessage = "Collection not found.";
        #endregion

        #region Vehicle Model Message
        public string FetchVehicleModelDetailsMessage = "Vehicle model details fetched successfully.";
        public string VehicleModelDetailsAddedMessage = "Vehicle model details added successfully.";
        public string VehicleModelDetailsUpdatedMessage = "Vehicle model details updated successfully.";
        public string VehicleModelDetailsRemoveMessage = "Vehicle model details removed successfully.";
        public string VehicleModelDetailsNotFoundMessage = "Vehicle model details not found.";
        #endregion

        #region Vehicle Type Message
        public string FetchVehicleTypeDetailsMessage = "Vehicle type details fetched successfully.";
        public string VehicleTypeDetailsAddedMessage = "Vehicle type details added successfully.";
        public string VehicleTypeDetailsUpdateMessage = "Vehicle type details updated successfully.";
        public string VehicleTypeDetailsRemoveMessage = "Vehicle type details removed successfully.";
        public string VehicleTypeDetailsNotFoundMessage = "Vehicle type details not found.";
        #endregion

        #region Vehicle Message
        public string FetchVehicleDetailsMessage = "Vehicle details fetched successfully.";
        public string VehicleDetailsAddedMessage = "Vehicle details added successfully.";
        public string VehicleDetailsUpdatedMessage = "Vehicle details updated successfully.";
        public string VehicleDetailsRemoveMessage = "Vehicle details removed successfully.";
        public string VehicleDetailsNotFoundMessage = "Vehicle details not found.";

        #endregion

        #region Supplier Message
        public string FetchSupplierDetailsMessage = "Supplier details fetched successfully.";
        public string SupplierDetailsAddedMessage = "Supplier details added successfully.";
        public string SupplierDetailsUpdateMessage = "Supplier details updated successfully.";
        public string SupplierDetailsRemoveMessage = "Supplier details removed successfully.";
        public string SupplierDetailsNotFoundMessage = "Supplier details not found.";
        #endregion

        #region Import Fitment Message
        public string ImportFitmentNotFoundMessage => "Import fitment not found.";
        public string FetchImportFitmentMessage => "Failed to fetch import fitments.";
        public string ImportFitmentRemoveMessage => "Import fitment removed successfully.";
        public string ImportFitmentDetailsUpdateMessage => "Import fitment details updated.";
        public string ImportFitmentErrorMessage => "An error occurred while processing import fitments.";
        public string ImportFitmentCheckMessage => "Fitment check completed.";
        #endregion

        #region OEM Message
        public string FetchOEMDetailsMessage = "OEM details fetched successfully.";
        public string OEMDetailsAddedMessage = "OEM details added successfully.";
        public string OEMDetailsUpdatedMessage = "OEM details updated successfully.";
        public string OEMDetailsRemoveMessage = "OEM details removed successfully.";
        public string OEMDetailsNotFoundMessage = "OEM details not found.";
        public string OEMNameAlreadyExistsMessage = "OEM name already exists.";
        #endregion

        public string OEMPartIsAvailableMessage = "OEM part is available for this vehicle.";
        public string OEMPartIsNotAvailableMessage = "OEM part is not available for this vehicle.";


		public string ExcelFileNotInS3ErrorMessage = "Excel file not found in S3 of this name.";


		public string S3UploadFailErrorMessage = "S3 Upload Failed.";
        public string ExcelFileNotFoundErrorMessage = "Excel file not found.";
        public string ImageExtensionErrorMessage = "Only image files (JPG, PNG) are allowed.";
		public string ExcelExtensionErrorMessage = "Only excel files (csv,xls,xlsx) are allowed.";



     
    }
}

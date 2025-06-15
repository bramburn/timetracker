#!/usr/bin/env python3
"""
Direct S3 test to verify bucket access and upload functionality
"""

import boto3
import sys
from botocore.exceptions import ClientError, NoCredentialsError

def test_s3_upload():
    """Test direct S3 upload to verify bucket access"""
    
    print("🧪 Testing direct S3 upload...")
    
    try:
        # Create S3 client with the same credentials as the API
        s3_client = boto3.client(
            's3',
            aws_access_key_id='AKIAROFGEXQYGRC2AR2I',
            aws_secret_access_key='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',
            region_name='us-east-1'
        )
        
        bucket_name = 'icelabz-timetracker-dev'
        test_key = 'test/test-upload.txt'
        test_content = b'This is a test upload from Python script'
        
        print(f"📤 Uploading test file to bucket: {bucket_name}")
        
        # Upload test file
        s3_client.put_object(
            Bucket=bucket_name,
            Key=test_key,
            Body=test_content,
            ContentType='text/plain'
        )
        
        print(f"✅ Successfully uploaded test file to S3!")
        
        # Generate URL
        url = f"https://{bucket_name}.s3.amazonaws.com/{test_key}"
        print(f"🔗 File URL: {url}")
        
        # Clean up test file
        try:
            s3_client.delete_object(Bucket=bucket_name, Key=test_key)
            print(f"🧹 Cleaned up test file")
        except Exception as e:
            print(f"⚠️  Warning: Could not delete test file: {str(e)}")
        
        return True
        
    except ClientError as e:
        error_code = e.response['Error']['Code']
        print(f"❌ S3 ClientError ({error_code}): {str(e)}")
        return False
    except NoCredentialsError:
        print("❌ AWS credentials not found")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {str(e)}")
        return False

def list_buckets():
    """List all accessible S3 buckets"""
    
    print("\n🪣 Listing accessible S3 buckets...")
    
    try:
        s3_client = boto3.client(
            's3',
            aws_access_key_id='AKIAROFGEXQYGRC2AR2I',
            aws_secret_access_key='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',
            region_name='us-east-1'
        )
        
        response = s3_client.list_buckets()
        
        print("📋 Available buckets:")
        for bucket in response['Buckets']:
            bucket_name = bucket['Name']
            created = bucket['CreationDate'].strftime('%Y-%m-%d %H:%M:%S')
            
            # Check if it's one of our TimeTracker buckets
            if 'timetracker' in bucket_name.lower():
                print(f"   ✅ {bucket_name} (created: {created}) - TimeTracker bucket")
            else:
                print(f"   📦 {bucket_name} (created: {created})")
        
        return True
        
    except Exception as e:
        print(f"❌ Error listing buckets: {str(e)}")
        return False

def main():
    """Main test function"""
    
    print("🚀 Direct S3 Connection Test")
    print("=" * 50)
    
    # List buckets first
    list_success = list_buckets()
    
    # Test upload
    upload_success = test_s3_upload()
    
    print("\n" + "=" * 50)
    print("📋 S3 Test Summary:")
    print(f"   Bucket Listing: {'✅ PASS' if list_success else '❌ FAIL'}")
    print(f"   Direct Upload: {'✅ PASS' if upload_success else '❌ FAIL'}")
    
    if list_success and upload_success:
        print("\n🎉 S3 connection is working correctly!")
        print("💡 The issue might be in the .NET API configuration or image processing")
        return 0
    else:
        print("\n⚠️  S3 connection issues detected")
        return 1

if __name__ == "__main__":
    sys.exit(main())

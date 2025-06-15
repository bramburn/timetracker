#!/usr/bin/env python3
"""
Script to create the required S3 buckets for TimeTracker application
"""

import boto3
import sys
from botocore.exceptions import ClientError, NoCredentialsError

def create_s3_bucket(bucket_name, region='us-east-1'):
    """Create an S3 bucket in the specified region"""
    
    try:
        # Create S3 client
        s3_client = boto3.client(
            's3',
            aws_access_key_id='AKIAROFGEXQYGRC2AR2I',
            aws_secret_access_key='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',
            region_name=region
        )
        
        print(f"🪣 Creating S3 bucket: {bucket_name}")
        
        # Create bucket
        if region == 'us-east-1':
            # For us-east-1, don't specify LocationConstraint
            s3_client.create_bucket(Bucket=bucket_name)
        else:
            # For other regions, specify LocationConstraint
            s3_client.create_bucket(
                Bucket=bucket_name,
                CreateBucketConfiguration={'LocationConstraint': region}
            )
        
        print(f"✅ Successfully created bucket: {bucket_name}")
        
        # Set bucket versioning (optional but recommended)
        try:
            s3_client.put_bucket_versioning(
                Bucket=bucket_name,
                VersioningConfiguration={'Status': 'Enabled'}
            )
            print(f"📦 Enabled versioning for bucket: {bucket_name}")
        except Exception as e:
            print(f"⚠️  Warning: Could not enable versioning: {str(e)}")
        
        # Set bucket encryption (optional but recommended)
        try:
            s3_client.put_bucket_encryption(
                Bucket=bucket_name,
                ServerSideEncryptionConfiguration={
                    'Rules': [
                        {
                            'ApplyServerSideEncryptionByDefault': {
                                'SSEAlgorithm': 'AES256'
                            }
                        }
                    ]
                }
            )
            print(f"🔒 Enabled encryption for bucket: {bucket_name}")
        except Exception as e:
            print(f"⚠️  Warning: Could not enable encryption: {str(e)}")
        
        return True
        
    except ClientError as e:
        error_code = e.response['Error']['Code']
        if error_code == 'BucketAlreadyExists':
            print(f"⚠️  Bucket {bucket_name} already exists (owned by someone else)")
            return False
        elif error_code == 'BucketAlreadyOwnedByYou':
            print(f"✅ Bucket {bucket_name} already exists and is owned by you")
            return True
        else:
            print(f"❌ Error creating bucket {bucket_name}: {str(e)}")
            return False
    except NoCredentialsError:
        print("❌ AWS credentials not found. Please check your credentials.")
        return False
    except Exception as e:
        print(f"❌ Unexpected error creating bucket {bucket_name}: {str(e)}")
        return False

def verify_bucket_access(bucket_name):
    """Verify that we can access the bucket"""
    
    try:
        s3_client = boto3.client(
            's3',
            aws_access_key_id='AKIAROFGEXQYGRC2AR2I',
            aws_secret_access_key='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',
            region_name='us-east-1'
        )
        
        # Try to list objects in the bucket
        s3_client.head_bucket(Bucket=bucket_name)
        print(f"✅ Successfully verified access to bucket: {bucket_name}")
        return True
        
    except ClientError as e:
        error_code = e.response['Error']['Code']
        if error_code == '404':
            print(f"❌ Bucket {bucket_name} does not exist")
        elif error_code == '403':
            print(f"❌ Access denied to bucket {bucket_name}")
        else:
            print(f"❌ Error accessing bucket {bucket_name}: {str(e)}")
        return False
    except Exception as e:
        print(f"❌ Unexpected error verifying bucket {bucket_name}: {str(e)}")
        return False

def main():
    """Main function to create S3 buckets"""
    
    print("🚀 TimeTracker S3 Bucket Creation Script")
    print("=" * 50)
    
    # Bucket names
    buckets = [
        'icelabz-timetracker-dev',
        'icelabz-timetracker-prod'
    ]
    
    success_count = 0
    
    # Create buckets
    for bucket_name in buckets:
        print(f"\n📋 Processing bucket: {bucket_name}")
        
        # First check if bucket already exists and is accessible
        if verify_bucket_access(bucket_name):
            print(f"✅ Bucket {bucket_name} is already available")
            success_count += 1
        else:
            # Try to create the bucket
            if create_s3_bucket(bucket_name):
                success_count += 1
    
    # Summary
    print("\n" + "=" * 50)
    print("📋 Bucket Creation Summary:")
    for bucket_name in buckets:
        status = "✅ READY" if verify_bucket_access(bucket_name) else "❌ FAILED"
        print(f"   {bucket_name}: {status}")
    
    if success_count == len(buckets):
        print(f"\n🎉 All {len(buckets)} buckets are ready!")
        print("✅ You can now test the screenshot upload endpoint")
        return 0
    else:
        print(f"\n⚠️  {len(buckets) - success_count} bucket(s) failed to create")
        print("❌ Please check the errors above and try again")
        return 1

if __name__ == "__main__":
    sys.exit(main())

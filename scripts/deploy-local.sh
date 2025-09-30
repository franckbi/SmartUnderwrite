#!/bin/bash

# SmartUnderwrite Local Deployment Script
# This script helps with local development and testing of the deployment process

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-dev}
ACTION=${2:-plan}

echo -e "${GREEN}SmartUnderwrite Local Deployment Script${NC}"
echo -e "Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "Action: ${YELLOW}$ACTION${NC}"
echo ""

# Check prerequisites
check_prerequisites() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    
    # Check if AWS CLI is installed and configured
    if ! command -v aws &> /dev/null; then
        echo -e "${RED}AWS CLI is not installed${NC}"
        exit 1
    fi
    
    # Check if Terraform is installed
    if ! command -v terraform &> /dev/null; then
        echo -e "${RED}Terraform is not installed${NC}"
        exit 1
    fi
    
    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Docker is not installed${NC}"
        exit 1
    fi
    
    # Check AWS credentials
    if ! aws sts get-caller-identity &> /dev/null; then
        echo -e "${RED}AWS credentials not configured${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✓ Prerequisites check passed${NC}"
}

# Build Docker image locally
build_image() {
    echo -e "${YELLOW}Building Docker image...${NC}"
    
    cd "$(dirname "$0")/.."
    
    # Build the API image
    docker build -t smartunderwrite-api:local -f SmartUnderwrite.Api/Dockerfile .
    
    echo -e "${GREEN}✓ Docker image built successfully${NC}"
}

# Deploy infrastructure
deploy_infrastructure() {
    echo -e "${YELLOW}Deploying infrastructure...${NC}"
    
    cd terraform
    
    # Initialize Terraform
    terraform init
    
    # Validate configuration
    terraform validate
    
    # Set JWT secret if not provided
    if [ -z "$TF_VAR_jwt_secret" ]; then
        export TF_VAR_jwt_secret="local-dev-jwt-secret-$(date +%s)"
        echo -e "${YELLOW}Using generated JWT secret for local development${NC}"
    fi
    
    # Use placeholder image for local testing
    export TF_VAR_container_image_uri="public.ecr.aws/docker/library/nginx:latest"
    
    case $ACTION in
        plan)
            terraform plan -var-file="environments/$ENVIRONMENT.tfvars"
            ;;
        apply)
            terraform plan -var-file="environments/$ENVIRONMENT.tfvars" -out=tfplan
            echo -e "${YELLOW}Review the plan above. Continue? (y/N)${NC}"
            read -r response
            if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
                terraform apply tfplan
                echo -e "${GREEN}✓ Infrastructure deployed successfully${NC}"
            else
                echo -e "${YELLOW}Deployment cancelled${NC}"
            fi
            ;;
        destroy)
            terraform plan -destroy -var-file="environments/$ENVIRONMENT.tfvars" -out=destroy-plan
            echo -e "${RED}This will destroy all infrastructure. Continue? (y/N)${NC}"
            read -r response
            if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
                terraform apply destroy-plan
                echo -e "${GREEN}✓ Infrastructure destroyed${NC}"
            else
                echo -e "${YELLOW}Destroy cancelled${NC}"
            fi
            ;;
        *)
            echo -e "${RED}Invalid action: $ACTION${NC}"
            echo "Valid actions: plan, apply, destroy"
            exit 1
            ;;
    esac
    
    cd ..
}

# Test deployment
test_deployment() {
    echo -e "${YELLOW}Testing deployment...${NC}"
    
    cd terraform
    
    # Get App Runner URL
    APP_RUNNER_URL=$(terraform output -raw app_runner_service_url 2>/dev/null || echo "")
    
    if [ -n "$APP_RUNNER_URL" ]; then
        echo -e "Testing API health endpoint: ${YELLOW}$APP_RUNNER_URL/healthz${NC}"
        
        # Wait for service to be ready
        echo "Waiting for service to be ready..."
        sleep 30
        
        # Test health endpoint
        if curl -f "$APP_RUNNER_URL/healthz" &> /dev/null; then
            echo -e "${GREEN}✓ API health check passed${NC}"
        else
            echo -e "${RED}✗ API health check failed${NC}"
        fi
        
        # Get CloudFront URL
        CLOUDFRONT_URL=$(terraform output -raw cloudfront_distribution_domain 2>/dev/null || echo "")
        if [ -n "$CLOUDFRONT_URL" ]; then
            echo -e "Frontend URL: ${YELLOW}https://$CLOUDFRONT_URL${NC}"
        fi
    else
        echo -e "${YELLOW}No App Runner URL found (infrastructure may not be deployed)${NC}"
    fi
    
    cd ..
}

# Show deployment info
show_info() {
    echo -e "${YELLOW}Deployment Information:${NC}"
    
    cd terraform
    
    if terraform show &> /dev/null; then
        echo ""
        echo -e "${GREEN}Terraform Outputs:${NC}"
        terraform output
        
        echo ""
        echo -e "${GREEN}Useful Commands:${NC}"
        echo "View logs: aws logs tail /aws/apprunner/smartunderwrite-$ENVIRONMENT-api/application --follow"
        echo "View dashboard: https://console.aws.amazon.com/cloudwatch/home?region=us-east-1#dashboards:name=smartunderwrite-$ENVIRONMENT-dashboard"
    else
        echo -e "${YELLOW}No infrastructure deployed${NC}"
    fi
    
    cd ..
}

# Main execution
main() {
    case $ACTION in
        plan|apply|destroy)
            check_prerequisites
            deploy_infrastructure
            if [ "$ACTION" = "apply" ]; then
                test_deployment
                show_info
            fi
            ;;
        test)
            test_deployment
            ;;
        info)
            show_info
            ;;
        build)
            check_prerequisites
            build_image
            ;;
        *)
            echo -e "${RED}Invalid action: $ACTION${NC}"
            echo ""
            echo "Usage: $0 [environment] [action]"
            echo ""
            echo "Environments: dev, prod"
            echo "Actions:"
            echo "  plan     - Show Terraform plan"
            echo "  apply    - Deploy infrastructure"
            echo "  destroy  - Destroy infrastructure"
            echo "  test     - Test deployed services"
            echo "  info     - Show deployment information"
            echo "  build    - Build Docker image locally"
            echo ""
            echo "Examples:"
            echo "  $0 dev plan"
            echo "  $0 dev apply"
            echo "  $0 prod destroy"
            echo "  $0 dev test"
            exit 1
            ;;
    esac
}

main